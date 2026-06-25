# REFACTOR: Adapt Code to Simplified Event Pool Config Format

**Date:** 2026-06-25
**Target:** Make all C# code work with the new simplified `EventPoolConfigs.json`
**Estimated files to touch:** 4–5 C# files + 3–4 JSON config changes

---

## 1. What Changed

The user has simplified `PoolConfigs/EventPoolConfigs.json`. Previously each item in an event banner carried the full `path` and `element-type` fields:

```json
// OLD format (still visible in the orphaned per-banner files like CyreneEventAvatarPoolConfig.json)
{
    "type": "Avatar",
    "rarity": "Gold",
    "name": "Cyrene",
    "path": "Remembrance",
    "element-type": "Ice"
}
```

The new consolidated `EventPoolConfigs.json` now only contains a **reference**:

```json
// NEW format — minimal reference
{
    "type": "Avatar",
    "rarity": "Gold",
    "name": "Cyrene"
}
```

The full data (path, element-type) lives in two **master pool files** that act as look-up tables:

| File | What it contains |
|---|---|
| `PoolConfigs/AllGoldPoolConfig.json` | Every Gold (5★) Avatar and Light Cone with full data |
| `PoolConfigs/OrdinaryPurplePoolConfig.json` | Every Purple (4★) Avatar and Light Cone with full data |

Event banners are merely **subsets** of these master pools.

---

## 2. Design Decision: Lookup Key

### 2.1 Composite key: `(type, rarity, name)`

The **English display name** (`name` field) is already used as the canonical identifier throughout the codebase:

- Localization uses `avatar.<Name>` and `lightcone.<Name>` keys in `LanguageConfigs/TextMap.json`
- `LocalizationService.GetItemName(string englishName)` tries `avatar.{name}` then `lightcone.{name}`
- All master pool entries use the same English name

**Decision:** Use `(type, rarity, name)` as the composite lookup key. Type and rarity narrow the search space; name disambiguates within that space.

#### Why not a new `id` field?

Names already serve as IDs (they match localization keys exactly). Adding a separate `id` field would require updating all three config files + TextMap.json, doubling the maintenance burden. The one downside — names with special characters (parentheses, ampersands) — is already handled in the localization layer.

### 2.2 Typo to fix

There is one name mismatch that **must be corrected** in `EventPoolConfigs.json` before the refactor works:

| File | Current text | Correct text (matching master pool) |
|---|---|---|
| `EventPoolConfigs.json` line 42 | `"A Serect Vow"` | `"A Secret Vow"` |
| `OrdinaryPurplePoolConfig.json` line 232 | `"A Secret Vow"` | *(correct — the master)* |

The event config has a typo ("Serect" instead of "Secret"). Fix it in `EventPoolConfigs.json` (both `cyrene_lc` and `phainon_lc` banners).

---

## 3. Architecture of the Refactor

```
┌──────────────────────────┐     ┌──────────────────────────┐
│ EventPoolConfigs.json     │     │ AllGoldPoolConfig.json    │
│ (simplified — name only) │     │ (full data with path,     │
│                          │     │  element-type)            │
└──────────┬───────────────┘     └──────────┬───────────────┘
           │                                │
           │  DataLoader.LoadEventPoolConfigs(...)
           │  ┌─────────────────────────────┘
           │  │  Loads both master dicts first
           │  │  For each simplified item from event config:
           │  │    → Lookup in master dict by (type, rarity, name)
           │  │    → Return full ItemData (with path + element)
           ▼  ▼
     ┌─────────────────┐
     │ EventPoolConfig  │────► MainViewModel.InitializeSystems()
     │ Entry.Items      │       passes enriched items to
     │ (full ItemData)  │       GachaSystem.LoadPools(...)
     └─────────────────┘
```

**Key insight:** The `GachaSystem` stores event items in separate lists (`eventGoldItemPool`, `eventPurpleItemPool`). It uses **reference equality** (`eventGoldItemPool.Contains(item)`) to determine whether a pulled item is "on-rate" vs "off-rate." Since the enriched items are distinct object instances (not the same objects as those in the ordinary/celestial pools), `Contains` will correctly return `true` only for items that came from the event pool — matching the existing behavior. **No changes to `GachaSystem.cs` are needed.**

---

## 4. Step-by-Step Instructions

### Step 1 — Fix the typo in `EventPoolConfigs.json`

**File:** `PoolConfigs/EventPoolConfigs.json`

Change all occurrences of `"A Serect Vow"` to `"A Secret Vow"`. There are exactly 2 occurrences (one in `cyrene_lc`, one in `phainon_lc`).

### Step 2 — Delete orphaned per-banner config files

**Directory:** `PoolConfigs/`

The following files were the old per-banner event configs (superseded by `EventPoolConfigs.json`). Delete them:

- `ArcherEventAvatarPoolConfig.json`
- `ArcherEventLightConePoolConfig.json`
- `CyreneEventAvatarPoolConfig.json`
- `CyreneEventLightConePoolConfig.json`
- `PhainonEventAvatarPoolConfig.json`
- `PhainonEventLightConePoolConfig.json`
- `SaberEventAvatarPoolConfig.json`
- `SaberEventLightConePoolConfig.json`

Also delete the copies in `HSR-Gacha-Simulator/bin/Release/net10.0-windows/PoolConfigs/` (same 8 files).

### Step 3 — Refactor `DataLoader.cs`

**File:** `HSR-Gacha-Simulator/DataLoader.cs`

This is the core change. The `LoadEventPoolConfigs` method must accept the master pool data and use it to enrich each simplified item.

#### 3a. Change the method signature

Replace the current signature:
```csharp
public static List<EventPoolConfigEntry> LoadEventPoolConfigs(string filePath)
```

With one that accepts the master dictionaries:
```csharp
public static List<EventPoolConfigEntry> LoadEventPoolConfigs(
    string filePath,
    Dictionary<(ItemType type, ItemRarity rarity), Dictionary<string, ItemData>> masterLookup)
```

The nested dictionary structure is:
- **Outer key:** `(type, rarity)` — e.g. `(Avatar, Gold)` or `(LightCone, Purple)`
- **Inner key:** `name` (the English display name)
- **Value:** the full `ItemData` object from the master pool

#### 3b. Build a helper to create the master lookup

Add a new public method:
```csharp
/// <summary>
/// Builds a lookup dictionary from gold + purple master pool files.
/// Call this ONCE in MainViewModel, then pass the result to LoadEventPoolConfigs.
/// </summary>
public static Dictionary<(ItemType, ItemRarity), Dictionary<string, ItemData>>
    BuildMasterLookup(string allGoldPath, string ordinaryPurplePath)
{
    var allGold   = LoadFromFile(allGoldPath);
    var allPurple = LoadFromFile(ordinaryPurplePath);
    var merged    = allGold.Concat(allPurple).ToList();

    var lookup = new Dictionary<(ItemType, ItemRarity), Dictionary<string, ItemData>>();

    foreach (var item in merged)
    {
        var key = (item.Type, item.Rarity);
        if (!lookup.ContainsKey(key))
            lookup[key] = new Dictionary<string, ItemData>();

        // If there's a duplicate name within the same (type, rarity) group,
        // later entries overwrite earlier ones (shouldn't happen, but be safe).
        lookup[key][item.Name] = item;
    }
    return lookup;
}
```

#### 3c. Rewrite the body of `LoadEventPoolConfigs`

```csharp
public static List<EventPoolConfigEntry> LoadEventPoolConfigs(
    string filePath,
    Dictionary<(ItemType, ItemRarity), Dictionary<string, ItemData>> masterLookup)
{
    string json = File.ReadAllText(filePath);
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    var dtos = JsonSerializer.Deserialize<List<EventPoolEntryDto>>(json, options);

    if (dtos == null)
        return new List<EventPoolConfigEntry>();

    var entries = new List<EventPoolConfigEntry>();
    foreach (var dto in dtos)
    {
        if (!dto.Enabled)
            continue;

        var items = new List<ItemData>(dto.Items.Count);
        foreach (var itemDto in dto.Items)
        {
            // Parse type and rarity from the simplified item
            var type   = ParseEnum<ItemType>(itemDto.Type);
            var rarity = ParseEnum<ItemRarity>(itemDto.Rarity);
            var name   = itemDto.Name ?? "";

            // Look up the full data from the master pools
            if (masterLookup.TryGetValue((type, rarity), out var nameDict)
                && nameDict.TryGetValue(name, out var fullItem))
            {
                // Use the master pool's ItemData — it has path + element
                items.Add(fullItem);
            }
            else
            {
                // Fallback: item not found in master pool — log warning,
                // create a partial ItemData (path/element = Unknown)
                // so the banner still loads without crashing.
                LogWarning($"Item '{name}' ({type}, {rarity}) from event config not found in master pools.");
                items.Add(new ItemData
                {
                    Type        = type,
                    Rarity      = rarity,
                    Name        = name,
                    Path        = PathType.Unknown,
                    ElementType = ElementType.Unknown
                });
            }
        }

        entries.Add(new EventPoolConfigEntry
        {
            BannerKey   = dto.BannerKey,
            BannerTitle = dto.BannerTitle,
            Items       = items
        });
    }
    return entries;
}
```

#### 3d. Add a warning logger

Add this private static helper at the bottom of `DataLoader`:
```csharp
private static void LogWarning(string message)
{
    try
    {
        string logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HSR-Gacha-Simulator");
        Directory.CreateDirectory(logDir);
        File.AppendAllText(
            Path.Combine(logDir, "error.log"),
            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [DataLoader] WARNING: {message}{Environment.NewLine}");
    }
    catch { /* best-effort */ }
}
```

#### 3e. The `ItemDataDto` class — no changes needed

The existing `ItemDataDto` has optional `Path` and `ElementType` properties. When deserializing the simplified event config (which lacks those fields), `JsonSerializer` will set them to `null`. The `ToItemData` helper already handles nulls gracefully by returning `PathType.Unknown` / `ElementType.Unknown`. Since we're now looking up from the master pool, the `ToItemData` method is no longer called for event pool items — but keep it for `LoadFromFile` (still used by the master pool files and blue pool).

### Step 4 — Refactor `MainViewModel.cs`

**File:** `HSR-Gacha-Simulator/MainViewModel.cs`

#### 4a. In `InitializeSystems()`, build the master lookup before loading event configs

Find the section that loads event configs (around line 362):

```csharp
// ── Event banners (data-driven) ───────────────────────────
var eventConfigs = DataLoader.LoadEventPoolConfigs(
    Path.Combine(poolDir, "EventPoolConfigs.json"));
```

Replace with:
```csharp
// ── Event banners (data-driven) ───────────────────────────
// Build master lookup from the Gold + Purple full-data files
var masterLookup = DataLoader.BuildMasterLookup(
    Path.Combine(poolDir, "AllGoldPoolConfig.json"),
    Path.Combine(poolDir, "OrdinaryPurplePoolConfig.json"));

var eventConfigs = DataLoader.LoadEventPoolConfigs(
    Path.Combine(poolDir, "EventPoolConfigs.json"),
    masterLookup);
```

### Step 5 — Localization: No changes needed

**File:** `LanguageConfigs/TextMap.json` — **no changes required.**

The localization system already uses `avatar.<Name>` and `lightcone.<Name>` keys where `<Name>` is the English display name. `LocalizationService.GetItemName()` tries both prefixes. Since the `name` field in the simplified config matches the localization key suffix exactly, everything works.

Double-check: every name in `EventPoolConfigs.json` must have a corresponding entry in `TextMap.json`. Running a quick diff:

| Event Config Name | TextMap key | Status |
|---|---|---|
| Cyrene | `avatar.Cyrene` | ✓ |
| Phainon | `avatar.Phainon` | ✓ |
| Archer | `avatar.Archer` | ✓ |
| Saber | `avatar.Saber` | ✓ |
| Evernight | `avatar.Evernight` | ✓ |
| March 7th | `avatar.March 7th` | ✓ |
| Arlan | `avatar.Arlan` | ✓ |
| Yukong | `avatar.Yukong` | ✓ |
| Guinaifen | `avatar.Guinaifen` | ✓ |
| Xueyi | `avatar.Xueyi` | ✓ |
| Misha | `avatar.Misha` | ✓ |
| This Love, Forever | `lightcone.This Love, Forever` | ✓ |
| A Secret Vow | `lightcone.A Secret Vow` | ✓ |
| Subscribe for More! | `lightcone.Subscribe for More!` | ✓ |
| Dance! Dance! Dance! | `lightcone.Dance! Dance! Dance!` | ✓ |
| Thus Burns the Dawn | `lightcone.Thus Burns the Dawn` | ✓ |
| The Hell Where Ideals Burn | `lightcone.The Hell Where Ideals Burn` | ✓ |
| A Thankless Coronation | `lightcone.A Thankless Coronation` | ✓ |
| To Evernight's Stars | `lightcone.To Evernight's Stars` | ✓ |
| Dream's Montage | `lightcone.Dream's Montage` | ✓ |
| After the Charmony Fall | `lightcone.After the Charmony Fall` | ✓ |

All have corresponding entries. **No TextMap changes needed.**

### Step 6 — Files that need NO changes (verified)

These files were checked and confirmed to not require any modifications:

| File | Why it's unaffected |
|---|---|
| `GachaSystem.cs` | Uses `ItemData` objects directly. `Contains()` checks are reference-equality based and still work because event pool items are distinct object instances. |
| `ItemData.cs` | The data model is unchanged. Path and ElementType properties remain. |
| `EventPoolConfigEntry.cs` | Still holds `List<ItemData>` — the items just arrive enriched now. |
| `HistoryItemDisplay.cs` | Reads `ItemData.Path`, `ItemData.ElementType` as before. |
| `IconLoader.cs` | Uses `PathType` / `ElementType` enums — unchanged. |
| `ElementTypeToBrushConverter.cs` | Uses `ElementType` enum — unchanged. |
| `RarityConverters.cs` | Uses `ItemRarity` enum — unchanged. |
| `LocExtension.cs` | Purely a WPF binding helper — unchanged. |
| `LocalizationService.cs` | `GetItemName()` uses name string — unchanged. |
| `MainWindow.xaml` / `.cs` | UI layer — no data model changes. |

---

## 5. Verification Checklist

After completing all steps, verify:

1. **Build:** `dotnet build` succeeds with zero errors and zero warnings.
2. **Launch:** App starts without crashing. The `Evernight` banners should show (they're `enabled: true`).
3. **Event banners display:** All 9 enabled banners appear in the banner strip with correct localized titles.
4. **Pull result display:** After pulling on an event banner, the result card shows:
   - Correct rarity stars (★ count)
   - Localized item name
   - **Path text and icon** (e.g. "Remembrance" with the Remembrance icon)
   - **Element text and color** (e.g. "Ice" in blue for Cyrene)
5. **Purple rate-up:** Pull on an event banner enough times to get purple items. Verify the purple event items (e.g. March 7th, Arlan, Yukong) appear. Check that the purple guarantee state (`IsPurpleGuaranteed`) toggles correctly.
6. **Gold rate-up:** Pull enough for gold items. Verify event gold items drop and off-rate (50/50 loss) is tracked correctly in the "Missed" stats.
7. **Language switch:** Switch between EN and ZH — item names, path names, and element names localize correctly.
8. **Check the log:** Look at `%LOCALAPPDATA%/HSR-Gacha-Simulator/error.log` — there should be zero warnings from `DataLoader` about items not found in master pools.

---

## 6. Summary of All Changes

| # | File | Action |
|---|---|---|
| 1 | `PoolConfigs/EventPoolConfigs.json` | Fix typo: `"A Serect Vow"` → `"A Secret Vow"` (2 occurrences) |
| 2 | `PoolConfigs/*Event*PoolConfig.json` | Delete 8 orphaned per-banner files |
| 3 | `HSR-Gacha-Simulator/bin/Release/.../PoolConfigs/*Event*PoolConfig.json` | Delete 8 orphaned copies in Release output |
| 4 | `HSR-Gacha-Simulator/DataLoader.cs` | Add `BuildMasterLookup()` method; rewrite `LoadEventPoolConfigs()` to accept and use master lookup; add `LogWarning()` helper |
| 5 | `HSR-Gacha-Simulator/MainViewModel.cs` | Build master lookup before calling `LoadEventPoolConfigs()`; pass it as second argument |

**Total files modified:** 2 C# + 1 JSON (+ 8 files deleted)
**Files NOT touched:** GachaSystem.cs, ItemData.cs, EventPoolConfigEntry.cs, all converters, localization, UI
