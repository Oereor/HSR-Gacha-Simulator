# REVISION 11 — Add "All Gold" Banner

> **Target audience:** Coder agent performing the implementation.
> **Banner concept:** An ordinary-pool banner whose gold item pool is the complete set of all 5★ items HSR has released (avatars + light cones). The purple and blue pools are the standard ordinary pools. No 50/50, no rate-up — just the widest possible standard banner.

---

## Files You Will Touch

| File | Action |
|------|--------|
| `HSR-Gacha-Simulator/HSR-Gacha-Simulator.csproj` | Add `AllGoldPoolConfig.json` to build output |
| `LanguageConfigs/TextMap.json` | Add missing item name translations + new banner UI label |
| `HSR-Gacha-Simulator/MainWindow.xaml` | Add a RadioButton for the new banner |
| `HSR-Gacha-Simulator/MainWindow.xaml.cs` | Instantiate the new system + wire it into the banner map |
| (or `HSR-Gacha-Simulator/MainViewModel.cs` if the MVVM refactor is already applied) | Same — add the system and mapping |

**Do NOT touch:**
- `GachaSystem.cs` (no logic changes needed — Ordinary type handles this case)
- Any other pool config JSON files
- `LocalizationService.cs`, `DataLoader.cs`, converters, etc.

---

## Step 1 — Add `AllGoldPoolConfig.json` to the .csproj

**File:** `HSR-Gacha-Simulator/HSR-Gacha-Simulator.csproj`

Add a content entry so the file is copied to the output directory at build time:

```xml
<Content Include="..\PoolConfigs\AllGoldPoolConfig.json">
  <Link>PoolConfigs\AllGoldPoolConfig.json</Link>
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

Place this inside the existing `<ItemGroup>` that contains the other pool config entries (around lines 12–60).

**Verification:** Build the project, check that `bin\Debug\net10.0-windows\PoolConfigs\AllGoldPoolConfig.json` exists.

---

## Step 2 — Merge New Item Name Translations into TextMap.json

**File:** `LanguageConfigs/TextMap.json`

The `AllGoldPoolConfig.json` includes items (both avatars and light cones) that aren't currently referenced by any existing pool config. Their names aren't in `TextMap.json` yet and will display as the raw English key (e.g., `avatar.Ashveil`) instead of the translated name.

`GoldItemsTextmap.json` contains the EN+ZH names for **all** gold items. You need to:

1. Open `LanguageConfigs/TextMap.json` and note which `avatar.*` and `lightcone.*` keys already exist.
2. Open `LanguageConfigs/GoldItemsTextmap.json`. For each entry:
   - If `text-type` is `"AvatarName"`, the key to add is `avatar.<name-EN>` (e.g. `avatar.Castorice`).
   - If `text-type` is `"LightConeName"`, the key to add is `lightcone.<name-EN>` (e.g. `lightcone.Before Dawn`).
3. If that key does **not** already exist in `TextMap.json`, add a new entry under `"entries"`:

```json
"avatar.Castorice": {
  "en": "Castorice",
  "zh": "遐蝶"
},
```

**Important format notes:**
- The entry key is `avatar.<name-EN>` or `lightcone.<name-EN>`. The `<name-EN>` must match the `"name"` field in the pool config JSON exactly (case-sensitive).
- The `"en"` value is the English display name (copied from `name-EN`). For avatars, it's usually the same as the name-EN, but some light cones may have minor formatting differences.
- The `"zh"` value is the Chinese translation (copied from `name-ZH`).
- Add entries in the same section as existing ones — `avatar.*` entries are at the top of the entries object, `lightcone.*` entries follow.
- Keep the JSON formatting consistent with surrounding entries (indentation, trailing commas, etc.).

**How to identify missing entries efficiently:** Write a quick script or use manual comparison. The pool config lists all `"name"` values — for each name, check if `avatar.<name>` or `lightcone.<name>` exists in TextMap.json. If not, find the corresponding entry in `GoldItemsTextmap.json` and add it.

**Verification:** After this step, every item name in `AllGoldPoolConfig.json` should resolve to a translated string (not fall through to the raw key). Run the app and pull a few times on the existing Ordinary banner — confirm existing item names still show correctly (no regression).

---

## Step 3 — Add the New Banner's Localization Key

**File:** `LanguageConfigs/TextMap.json`

Add a new entry under the `ui.banner.*` group (around line 587, after `ui.banner.saber_lc`):

```json
"ui.banner.all_gold": {
  "en": "All Gold (Expanded Pool)",
  "zh": "全五星（扩充池）"
},
```

> You can change the label text if you have a better name. Just keep the key as `ui.banner.all_gold`.

**Verification:** Not testable yet — will be verified in Step 5.

---

## Step 4 — Add the RadioButton in MainWindow.xaml

**File:** `HSR-Gacha-Simulator/MainWindow.xaml`

There are currently two rows of banner RadioButtons: Event Warps (5 buttons) and Collab Warps (4 buttons). Add a third row for the new banner below the Collab Warps row.

Locate the closing `</StackPanel>` of the Collab Warps row and the closing `</StackPanel>` of the outer banner container, and insert:

```xaml
<!-- All Gold row -->
<StackPanel Orientation="Horizontal" HorizontalAlignment="Center"
            Margin="0,6,0,0">
    <RadioButton x:Name="rbAllGold"
                 Content="{local:Loc ui.banner.all_gold}"
                 Style="{StaticResource RadioBannerStyle}"
                 GroupName="Banner"
                 Checked="Banner_Changed" />
</StackPanel>
```

Place it right before the inner `</StackPanel>` that closes the banner StackPanel (just before line 395 in the current file), and after the `</StackPanel>` closing the Collab Warps row.

**Important:** The `GroupName="Banner"` and `Checked="Banner_Changed"` must match exactly so the existing event handler picks it up.

**Verification:** After Step 5, the new radio button appears and is clickable.

---

## Step 5 — Instantiate the System and Wire It Up

**File:** `HSR-Gacha-Simulator/MainWindow.xaml.cs` (or `MainViewModel.cs` if the MVVM refactor is applied)

### 5a. Declare the system field

Add a new field alongside the other 9 systems (around line 25):

```csharp
private GachaSystem allGoldSystem = null!;
```

### 5b. Load the pool and create the system

In `InitializeSystems()` (or equivalent), add the following after the existing 9 systems are created:

```csharp
// 10. All-Gold banner (ordinary type with the full gold pool)
var allGoldRaw = DataLoader.LoadFromFile(Path.Combine(poolDir, "AllGoldPoolConfig.json"));
var allGoldGoldAvatars    = allGoldRaw.Where(i => i.Type == ItemType.Avatar).ToList();
var allGoldGoldLightCones = allGoldRaw.Where(i => i.Type == ItemType.LightCone).ToList();

allGoldSystem = GachaSystem.Create(GachaType.Ordinary);
allGoldSystem.LoadPools(
    allGoldGoldAvatars, allGoldGoldLightCones,
    celestialGoldAvatars: new List<ItemData>(),
    eventGoldItems:   new List<ItemData>(),
    purpleAvatars, purpleLightCones,
    eventPurpleItems: new List<ItemData>(),
    blueItems);
```

**Key points:**
- Uses `GachaType.Ordinary` — no 50/50, no rate-up. Same probability model as the standard ordinary banner.
- Purple and blue pools are the shared `purpleAvatars`, `purpleLightCones`, and `blueItems` already loaded for the ordinary banner.
- `celestialGoldAvatars` and `eventGoldItems` are empty — this is an ordinary banner with no event items.
- The gold pool uses the newly loaded `allGoldGoldAvatars` and `allGoldGoldLightCones` from `AllGoldPoolConfig.json`.

### 5c. Add the system to the banner mapping

**If the if-else refactor (Step 6 of Refactor_Plan.md) has been applied** — add one entry to the `_bannerMap` dictionary:

```csharp
{ rbAllGold, new(allGoldSystem, "ui.banner.all_gold") },
```

**If the old if-else chain is still in place** — add a branch in both `Banner_Changed` and `GetBannerKey()`:

In `Banner_Changed` (before the closing brace):
```csharp
else if (rbAllGold.IsChecked == true)
    currentSystem = allGoldSystem;
```

In `GetBannerKey()`:
```csharp
if (currentSystem == allGoldSystem)
    return "ui.banner.all_gold";
```

### 5d. If banner-switch logic exists elsewhere

Make sure the new system participates in any centralized banner-switch logic. If there's a `SwitchBanner()` method in the ViewModel, the above mapping should cover it automatically.

**Verification:** Build succeeds. Click the "All Gold" radio button — the banner switches. Pull ×1 and ×10 — items come from the expanded gold pool. History and statistics display correctly. Reset works. The banner label in the reset confirmation dialog shows the correct name.

---

## Step 6 — Manual Test Checklist

- [ ] App builds and launches without errors
- [ ] The "All Gold (Expanded Pool)" radio button appears in its own row below the collab banners
- [ ] Clicking it switches the active banner (pity counters reset to 0 for the new banner)
- [ ] Switching to another banner and back preserves each banner's independent state
- [ ] Pull ×1 produces results from the expanded gold pool
- [ ] Pull ×10 produces results with guaranteed purple-or-better
- [ ] All pulled item names display correctly in English and Chinese (no raw keys like `avatar.Ashveil`)
- [ ] Result card shows correct rarity, path icon, element icon, element color
- [ ] History list shows correct item details
- [ ] Statistics panel updates correctly (total pulls, 5★/4★/3★ counts)
- [ ] Reset shows confirmation dialog with "All Gold (Expanded Pool)"
- [ ] Reset clears history and pity for this banner only (other banners unaffected)

---

## Appendix — Items Needing New TextMap Entries

The following items appear in `AllGoldPoolConfig.json` and are likely **not** in the current `TextMap.json`. Verify each and add the missing ones.

### Avatars (check `avatar.<name>`)

From `GoldItemsTextmap.json`, all avatar entries. Common new additions include (but are not limited to):

- `avatar.Castorice` → "遐蝶"
- `avatar.Cerydra` → "刻律德菈"
- `avatar.Cipher` → "赛飞儿"
- `avatar.Ashveil` → "不死途"
- `avatar.Evanescia` → "绯英"
- `avatar.Evernight` → "长夜月"
- `avatar.Hyacine` → "风堇"
- `avatar.Hysilens` → "海瑟音"
- `avatar.Mydei` → "万敌"
- `avatar.Mortenax Blade` → "千冶·刃"
- `avatar.The Dahlia` → "大丽花"
- `avatar.Yaoguang` → "爻光"
- `avatar.Tribbie` → "缇宝"
- `avatar.Sparkle` → "花火"
- `avatar.Sparxie` → "火花"
- `avatar.Silver Wolf LV.999` → "银狼 LV.999"
- `avatar.Dan Heng (Permansor Terrae)` → "丹恒·腾荒"
- `avatar.Rappa` → "乱破"
- `avatar.Jiaoqiu` → "椒丘"

### Light Cones (check `lightcone.<name>`)

From `GoldItemsTextmap.json`, all light cone entries. Common new additions:

- `lightcone.A Grounded Ascent` → "回到大地的飞行"
- `lightcone.A Thankless Coronation` → "没有回报的加冕"
- `lightcone.An Instant Before A Gaze` → "片刻，留在眼底"
- `lightcone.Baptism of Pure Thought` → "纯粹思维的洗礼"
- `lightcone.Before Dawn` → "拂晓之前"
- `lightcone.Brighter Than the Sun` → "比阳光更明亮的"
- `lightcone.Dance at Sunset` → "落日时起舞"
- `lightcone.Dazzled by a Flowery World` → "花花世界迷人眼"
- `lightcone.Earthly Escapade` → "游戏尘寰"
- `lightcone.Echoes of the Coffin` → "棺的回响"
- `lightcone.Epoch Etched in Golden Blood` → "金血铭刻的时代"
- `lightcone.Flame of Blood, Blaze My Path` → "血火啊，燃烧前路"
- `lightcone.Flowing Nightglow` → "夜色流光溢彩"
- `lightcone.I Shall Be My Own Sword` → "此身为剑"
- `lightcone.I Venture Forth to Hunt` → "我将，巡征追猎"
- `lightcone.If Time Were a Flower` → "如果时间是一朵花"
- `lightcone.In The Night` → "于夜色中"
- `lightcone.Incessant Rain` → "雨一直下"
- `lightcone.Inherently Unjust Destiny` → "命运从未公平"
- `lightcone.Into the Unreachable Veil` → "向着不可追问处"
- `lightcone.Lies Dance on the Breeze` → "谎言在风中飘扬"
- `lightcone.Life Should Be Cast to Flames` → "生命当付之一炬"
- `lightcone.Long May Rainbows Adorn the Sky` → "愿虹光永驻天空"
- `lightcone.Long Road Leads Home` → "长路终有归途"
- `lightcone.Make Farewells More Beautiful` → "让告别，更美一些"
- `lightcone.Never Forget Her Flame` → "勿忘她的火焰"
- `lightcone.Night of Fright` → "惊魂夜"
- `lightcone.Ninjutsu Inscription: Dazzling Evilbreaker` → "忍法帖·缭乱破魔"
- `lightcone.Past Self in Mirror` → "镜中故我"
- `lightcone.Patience Is All You Need` → "只需等待"
- `lightcone.Reforged Remembrance` → "重塑时光之忆"
- `lightcone.Reforged in Hellfire` → "灼尽炼狱的新骸"
- `lightcone.Sailing Towards a Second Life` → "驶向第二次生命"
- `lightcone.Scent Alone Stays True` → "唯有香如故"
- `lightcone.She Already Shut Her Eyes` → "她已闭上双眼"
- `lightcone.The Finale of a Lie` → "一场谎言的终幕"
- `lightcone.The Hell Where Ideals Burn` → "理想燃烧的地狱"
- `lightcone.The Unreachable Side` → "到不了的彼岸"
- `lightcone.This Love, Forever` → "爱如此刻永恒"
- `lightcone.Those Many Springs` → "那无数个春天"
- `lightcone.Though Worlds Apart` → "纵然山河万程"
- `lightcone.Thus Burns the Dawn` → "黎明恰如此燃烧"
- `lightcone.Time Woven Into Gold` → "将光阴织成黄金"
- `lightcone.To Evernight's Stars` → "致长夜的星光"
- `lightcone.Until the Flowers Bloom Again` → "邂逅于下一个花季"
- `lightcone.Welcome to the Cosmic City` → "欢迎来到银河城"
- `lightcone.When She Decided to See` → "当她决定看见"
- `lightcone.Whereabouts Should Dreams Rest` → "梦应归于何处"
- `lightcone.Why Does the Ocean Sing` → "海洋为何而歌"
- `lightcone.Worrisome, Blissful` → "烦恼着，幸福着"
- `lightcone.Yet Hope Is Priceless` → "偏偏希望无价"
- `lightcone.Along the Passing Shore` → "行于流逝的岸"