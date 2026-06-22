# Result Card Icons — Implementation Spec

## Overview

Add Path and Element icons to the result/preview card in the "Latest Pull Result" panel. The Path icon appears to the left of the item name; the Element icon appears to the right. For Light Cones (which have no element), the right-side slot is left empty.

---

## 1. Icon Assets

### 1.1 Location

All icons are in `Icons\` at the repo root (adjacent to `LanguageConfigs\` and `PoolConfigs\`).

### 1.2 Naming Convention

```
Path_<EnumValue>.png      Element_<EnumValue>.png
```

The `<EnumValue>` portion matches the C# enum member name exactly (PascalCase):

| File | Enum |
|---|---|
| `Path_Destruction.png` | `PathType.Destruction` |
| `Path_TheHunt.png` | `PathType.TheHunt` |
| `Path_Erudition.png` | `PathType.Erudition` |
| `Path_Harmony.png` | `PathType.Harmony` |
| `Path_Nihility.png` | `PathType.Nihility` |
| `Path_Preservation.png` | `PathType.Preservation` |
| `Path_Abundance.png` | `PathType.Abundance` |
| `Path_Rememberance.png` | `PathType.Remembrance` ⚠️ |
| `Path_Elation.png` | `PathType.Elation` |
| `Element_Physical.png` | `ElementType.Physical` |
| `Element_Fire.png` | `ElementType.Fire` |
| `Element_Ice.png` | `ElementType.Ice` |
| `Element_Lightning.png` | `ElementType.Lightning` |
| `Element_Wind.png` | `ElementType.Wind` |
| `Element_Quantum.png` | `ElementType.Quantum` |
| `Element_Imaginary.png` | `ElementType.Imaginary` |

> ⚠️ **Filename mismatch:** The icon file is named `Path_Rememberance.png` (with an 'a') but the C# enum is `PathType.Remembrance` (no 'a'). The icon-loading code must account for this discrepancy — either rename the file or handle the mapping in code. **Prefer renaming the file** to `Path_Remembrance.png` so the naming rule is consistent and no special-case code is needed.

### 1.3 Build Output

Icons must be copied to the output directory so they are available at runtime. The path at runtime will be:
```
<BaseDirectory>\Icons\Path_Destruction.png
```

---

## 2. Current Result Card Layout (Reference)

The result card is defined in `MainWindow.xaml` lines 573–627. Its structure:

```
Border (resultCardBorder)
  StackPanel (centered)
    TextBlock (txtResultRarity)     ← "★★★★★"
    TextBlock (txtResultName)       ← "Himeko"
    StackPanel (horizontal)         ← type / path / element row
      TextBlock (txtResultType)     ← "Avatar"
      TextBlock (separator)         ← " · "
      TextBlock (txtResultPath)     ← "Erudition"
      TextBlock (dotElement)        ← " · " (hidden for Light Cones)
      TextBlock (txtResultElement)  ← "Fire"
    TextBlock (txtResultPullNum)    ← "Pull #5 of 50"
```

The name row (line 591–596) is a single `TextBlock`:
```xml
<TextBlock x:Name="txtResultName"
           Text="{local:Loc ui.result.default}"
           FontSize="22" FontWeight="Bold"
           Foreground="#e0e0e0"
           HorizontalAlignment="Center" />
```

---

## 3. Target Layout

The name row changes from a single centered `TextBlock` to a horizontal `StackPanel` with three children:

```
[PathIcon]  [Name]  [ElementIcon]
  32×32     centered   32×32 (or empty if Light Cone)
```

```
Border (resultCardBorder)
  StackPanel (centered)
    TextBlock (txtResultRarity)     ← unchanged
    StackPanel (horizontal, centered)  ← NEW: name row with icons
      Image (imgResultPath)         ← NEW: 32×32, left of name
      TextBlock (txtResultName)     ← unchanged
      Image (imgResultElement)      ← NEW: 32×32, right of name
    StackPanel (horizontal)         ← unchanged type/path/element row
      ...
    TextBlock (txtResultPullNum)    ← unchanged
```

---

## 4. Required Changes

### 4.1 Project File (`HSR-Gacha-Simulator.csproj`)

Add the entire `Icons\` directory as content:

```xml
<Content Include="..\Icons\*.*">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  <Link>Icons\%(FileName)%(Extension)</Link>
</Content>
```

This mirrors how `PoolConfigs\` is already included. The `Link` metadata places the files under `Icons\` in the output directory regardless of the source path traversal.

### 4.2 `MainWindow.xaml` — Result Card Name Row

Replace the single `TextBlock` for `txtResultName` with a horizontal `StackPanel` containing the three elements:

**Requirements for the new row:**
- `Orientation="Horizontal"`, `HorizontalAlignment="Center"`
- The Path `Image` (`x:Name="imgResultPath"`):
  - Fixed size: `Width="32" Height="32"`, `Margin="0,0,8,0"`
  - `VerticalAlignment="Center"`
  - `Visibility="Visible"` by default
- The Name `TextBlock` (`txtResultName`):
  - Keep all existing properties (`FontSize`, `FontWeight`, `Foreground`, `HorizontalAlignment`)
  - Remove the localization markup extension on `Text` (it gets set from code-behind)
- The Element `Image` (`x:Name="imgResultElement"`):
  - Fixed size: `Width="32" Height="32"`, `Margin="8,0,0,0"`
  - `VerticalAlignment="Center"`
  - `Visibility="Visible"` by default; collapsed/hidden for Light Cones

### 4.3 `MainWindow.xaml.cs` — `ShowResultAtIndex()`

After the existing code sets `txtResultName.Text`, add icon logic:

```
Given the pulled ItemData item:
  1. Path icon:
     - Construct filename: $"Icons/Path_{item.Path}.png"
     - Load into a BitmapImage, assign to imgResultPath.Source
     - If the file is missing, set imgResultPath.Visibility = Collapsed
  2. Element icon:
     - If item.Type == LightCone OR item.ElementType == Unknown:
         imgResultElement.Visibility = Collapsed
         imgResultElement.Source = null
     - Else:
         Construct filename: $"Icons/Element_{item.ElementType}.png"
         Load into a BitmapImage, assign to imgResultElement.Source
         imgResultElement.Visibility = Visible
```

**BitmapImage loading notes:**
- Use a relative path from `AppDomain.CurrentDomain.BaseDirectory`.
- WPF `BitmapImage` needs a `Uri` — prefer `new BitmapImage(new Uri(fullPath))`.
- If the PNG file doesn't exist, catch the exception (or check with `File.Exists`) and collapse the image rather than crashing.
- Consider caching loaded `BitmapImage` instances in a static dictionary keyed by path to avoid re-reading from disk on every pull.

### 4.4 `MainWindow.xaml.cs` — `ClearResultCard()`

Reset both icon images:
- `imgResultPath.Source = null` (and `Visibility = Collapsed`)
- `imgResultElement.Source = null` (and `Visibility = Collapsed`)

### 4.5 Icon File Rename (Recommended)

Rename `Icons\Path_Rememberance.png` → `Icons\Path_Remembrance.png` to match the `PathType.Remembrance` enum value. This eliminates the need for a special-case mapping. The git history will track the rename.

---

## 5. Icon Cache (Implementation Note)

Loading PNG files from disk on every pull is unnecessary I/O. A reasonable approach:
- Maintain a `private static readonly Dictionary<string, BitmapImage>` cache in `MainWindow`.
- Key = full file path; populate on first access.
- Since the icon set is small (16 files, ~50 KB total), memory overhead is negligible.

This is an optimization detail and not required for correctness — direct file loading per pull is acceptable if simpler.

---

## 6. Files Summary

| File | Action |
|---|---|
| `Icons\Path_Rememberance.png` | **RENAME** → `Path_Remembrance.png` |
| `HSR-Gacha-Simulator\HSR-Gacha-Simulator.csproj` | **MODIFY** — add `Icons\*.*` as content |
| `HSR-Gacha-Simulator\MainWindow.xaml` | **MODIFY** — restructure name row to include icon Images |
| `HSR-Gacha-Simulator\MainWindow.xaml.cs` | **MODIFY** — set icon sources in `ShowResultAtIndex` / `ClearResultCard` |

No other files should be touched.

---

## 7. Acceptance Criteria

1. **Path icon visible:** Pulling any item shows the correct Path icon to the left of the item name (32×32, centered vertically with the name).
2. **Element icon visible:** Pulling an Avatar shows the correct Element icon to the right of the item name.
3. **Light Cone no element:** Pulling a Light Cone shows the Path icon on the left but no Element icon on the right (the element `Image` is collapsed, and the layout doesn't leave a gap or shift).
4. **Unknown element:** Items with `ElementType.Unknown` behave the same as Light Cones for the element icon.
5. **Result card cleared:** When no pull result is shown, both icons are hidden (not showing stale icons from a previous pull).
6. **Navigation works:** Browsing history with ◄/► buttons updates both icons correctly for each historical pull.
7. **Banner switch works:** Switching banners resets the result card and hides both icons.
8. **Build output:** `Icons\*.png` files appear in the build output directory alongside the executable.

---

## 8. Visual Reference

```
┌──────────────────────────────────────────┐
│              ★★★★★                       │
│                                          │
│   🔴    Himeko    🔥                     │
│         Avatar · Erudition · Fire        │
│         Pull #5 of 50                    │
│                                          │
│       ◄ Previous    Next ►               │
└──────────────────────────────────────────┘

┌──────────────────────────────────────────┐
│              ★★★★★                       │
│                                          │
│   🔴    Night on the Milky Way           │
│         Light Cone · Erudition · —       │
│         Pull #12 of 50                   │
│                                          │
│       ◄ Previous    Next ►               │
└──────────────────────────────────────────┘
```

*(🔴 = Path icon placeholder; 🔥 = Element icon placeholder. Actual icons are the PNG files.)*
