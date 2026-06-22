# Internationalization & Localization — Implementation Spec

## Overview

Add Chinese (ZH) localization to the HSR Gacha Simulator with runtime language switching between English (EN) and Chinese (ZH). The system must be extensible to additional languages with zero code changes.

---

## 1. Key Design Decisions (Already Made — Do Not Revisit)

| Decision | Rationale |
|---|---|
| **Human-readable dot-separated keys** (not hashed IDs) | Hashed IDs (CRC32/MD5 of English text, as used in miHoYo's game engine) solve obfuscation and build-pipeline determinism for shipped games. For a desktop app, readable keys are debuggable, maintainable, and require no pre-build tooling. |
| **Single JSON file** (`TextMap.json`) | One load, one source of truth, simpler version-control diff. |
| **Flat `entries` dictionary** (not nested JSON) | Faster lookup, easier to diff, trivial to index in C#. |
| **JSON, not .NET RESX** | JSON is tooling-agnostic, already used by the project, and supports runtime language switching without restart. RESX requires designer tooling and complicates runtime switching. |
| **WPF MarkupExtension** for XAML strings | `{local:Loc key}` is concise, readable, and supports live language switching via binding. |
| **Item names keyed by English name** | `ItemData.Name` is the English name. No need to retrofit a numeric ID onto every item. |

---

## 2. TextMap File

### 2.1 Location & Format

**File:** `LanguageConfigs/TextMap.json` (new file; the 4 existing `*Textmap.json` files remain as reference data and are not loaded at runtime.)

**Schema:**

```jsonc
{
  "meta": {
    "version": 1,
    "languages": ["en", "zh"],
    "defaultLanguage": "en"
  },
  "entries": {
    "<key>": {
      "en": "<English text>",
      "zh": "<Chinese text>"
    }
  }
}
```

**Constraints:**
- `meta.languages` — ordered list of supported ISO 639-1 language codes. The runtime reads available languages from this list.
- `meta.defaultLanguage` — fallback when a key is missing a translation for the current language. The service must fall back through: current language → default language → raw key (never throw on a missing translation).
- `entries` — flat dictionary. Keys are case-sensitive.
- Runtime substitution placeholders use .NET format strings (`{0}`, `{1}`, …).

### 2.2 Key Naming Convention

```
namespace.category.specific_name
```

| Namespace | Scope | Key format | Example |
|---|---|---|---|
| `ui.*` | Window chrome, labels, buttons, status bar, grid headers | `snake_case` after namespace | `ui.button.warp_x1` |
| `meta.*` | Game-system terminology | `snake_case` after namespace | `meta.ordinary_pool` |
| `path.*` | `PathType` enum → display name | `PathType` member name as-is | `path.Destruction` |
| `element.*` | `ElementType` enum → display name | `ElementType` member name as-is | `element.Fire` |
| `avatar.*` | Avatar display name | English in-game name as-is | `avatar.March 7th` |
| `lightcone.*` | Light Cone display name | English in-game name as-is | `lightcone.Night on the Milky Way` |
| `dialog.*` | MessageBox title/body text | `snake_case` after namespace | `dialog.reset_banner.message` |

### 2.3 Required Key Inventory

The `TextMap.json` must contain every key listed below. Translations for `avatar.*` and `lightcone.*` keys can be generated from the 4 existing LanguageConfigs files. UI and dialog keys require new ZH translations (provided in the tables).

#### Window & GroupBox Headers
| Key | EN | ZH |
|---|---|---|
| `ui.window.title` | HSR Gacha Simulator | 星穹铁道抽卡模拟器 |
| `ui.group.banner_selection` | Banner Selection | 卡池选择 |
| `ui.group.pull_controls` | Pull Controls | 跃迁控制 |
| `ui.group.statistics` | Statistics | 统计数据 |
| `ui.group.latest_result` | Latest Pull Result | 最新跃迁结果 |
| `ui.group.pull_history` | Pull History | 跃迁历史 |

#### Banner RadioButton Labels
| Key | EN | ZH |
|---|---|---|
| `ui.banner.ordinary` | Ordinary | 常驻 |
| `ui.banner.cyrene_avatar` | Cyrene (Avatar) | 昔涟（角色） |
| `ui.banner.phainon_avatar` | Phainon (Avatar) | 白厄（角色） |
| `ui.banner.cyrene_lc` | Cyrene (LC) | 昔涟（光锥） |
| `ui.banner.phainon_lc` | Phainon (LC) | 白厄（光锥） |
| `ui.banner.archer_avatar` | Archer (Avatar) | Archer（角色） |
| `ui.banner.archer_lc` | Archer (LC) | Archer（光锥） |
| `ui.banner.saber_avatar` | Saber (Avatar) | Saber（角色） |
| `ui.banner.saber_lc` | Saber (LC) | Saber（光锥） |

#### Button Labels
| Key | EN | ZH |
|---|---|---|
| `ui.button.warp_x1` | Warp ×1 | 跃迁×1 |
| `ui.button.warp_x10` | Warp ×10 | 跃迁×10 |
| `ui.button.reset_banner` | Reset Banner | 重置卡池 |
| `ui.button.prev_page` | ◄ Previous | ◄ 上一个 |
| `ui.button.next_page` | Next ► | 下一个 ► |

#### Pity Display Labels
| Key | EN | ZH |
|---|---|---|
| `ui.pity.gold_pity` | Gold pity: | 五星保底： |
| `ui.pity.gold_since` | pulls since last 5★ | 抽未出五星 |
| `ui.pity.gold_guarantee` | Gold guarantee: | 五星保底状态： |
| `ui.pity.purple_pity` | Purple pity: | 四星保底： |
| `ui.pity.purple_since` | pulls since last 4★ | 抽未出四星 |
| `ui.pity.purple_guarantee` | Purple guarantee: | 四星保底状态： |
| `ui.pity.guaranteed` | ✅ Guaranteed | ✅ 已保底 |
| `ui.pity.not_guaranteed` | Not Guaranteed | 未保底 |

#### Statistics Labels
| Key | EN | ZH |
|---|---|---|
| `ui.stats.total_pulls` | Total Pulls: {0} | 总跃迁次数：{0} |
| `ui.stats.rarity_5star` | 5★ Gold | 5★ 金 |
| `ui.stats.rarity_4star` | 4★ Purple | 4★ 紫 |
| `ui.stats.rarity_3star` | 3★ Blue | 3★ 蓝 |

#### Result Card
| Key | EN | ZH |
|---|---|---|
| `ui.result.default` | Pull to begin | 点击跃迁开始 |
| `ui.result.pull_number` | Pull #{0} of {1} | 第{0}抽 / 共{1}抽 |
| `ui.result.separator` | · | · |
| `ui.result.type.avatar` | Avatar | 角色 |
| `ui.result.type.lightcone` | Light Cone | 光锥 |

#### History Grid
| Key | EN | ZH |
|---|---|---|
| `ui.history.header.number` | # | # |
| `ui.history.header.name` | Name | 名称 |
| `ui.history.header.rarity` | Rarity | 稀有度 |
| `ui.history.header.type` | Type | 类型 |
| `ui.history.header.path` | Path | 命途 |
| `ui.history.header.element` | Element | 属性 |
| `ui.history.type.avatar` | Avatar | 角色 |
| `ui.history.type.lightcone_short` | L.Cone | 光锥 |

#### Status Bar
| Key | EN | ZH |
|---|---|---|
| `ui.status.ready` | Ready | 就绪 |
| `ui.status.pulling` | Pulling... | 跃迁中… |
| `ui.status.pulling_x10` | Pulling ×10... | 跃迁×10中… |
| `ui.status.init_failed` | Initialisation failed | 初始化失败 |
| `ui.status.banner_reset` | {0} banner reset | {0}卡池已重置 |

#### Dialogs
| Key | EN | ZH |
|---|---|---|
| `dialog.reset_banner.title` | Reset Banner | 重置卡池 |
| `dialog.reset_banner.message` | Reset the {0} banner? All pull history and pity for this banner will be lost. | 确定重置{0}卡池？该卡池的所有跃迁记录和保底数据将被清除。 |
| `dialog.error.title` | Error | 错误 |
| `dialog.error.init_failed` | Failed to initialise: {0} | 初始化失败：{0} |

#### Game Terminology (Meta)
| Key | EN | ZH |
|---|---|---|
| `meta.path` | Path | 命途 |
| `meta.element` | Element | 属性 |
| `meta.rarity` | Rarity | 稀有度 |
| `meta.pool` | Pool | 卡池 |
| `meta.ordinary_pool` | Ordinary Pool | 常驻卡池 |
| `meta.event_pool` | Event Pool | 活动卡池 |
| `meta.warp` | Warp | 跃迁 |
| `meta.avatar` | Avatar | 角色 |
| `meta.light_cone` | Light Cone | 光锥 |

#### Path Names
Key = `PathType` enum member name. One entry per enum value:

| Key | EN | ZH |
|---|---|---|
| `path.Destruction` | Destruction | 毁灭 |
| `path.TheHunt` | The Hunt | 巡猎 |
| `path.Erudition` | Erudition | 智识 |
| `path.Harmony` | Harmony | 同谐 |
| `path.Nihility` | Nihility | 虚无 |
| `path.Preservation` | Preservation | 存护 |
| `path.Abundance` | Abundance | 丰饶 |
| `path.Remembrance` | Remembrance | 记忆 |
| `path.Elation` | Elation | 欢愉 |

#### Element Names
Key = `ElementType` enum member name. One entry per enum value:

| Key | EN | ZH |
|---|---|---|
| `element.Physical` | Physical | 物理 |
| `element.Fire` | Fire | 火 |
| `element.Ice` | Ice | 冰 |
| `element.Lightning` | Lightning | 雷 |
| `element.Wind` | Wind | 风 |
| `element.Quantum` | Quantum | 量子 |
| `element.Imaginary` | Imaginary | 虚数 |

#### Avatar & Light Cone Names

Keys follow the pattern `avatar.<EnglishName>` and `lightcone.<EnglishName>`. Source data is in the 4 existing `LanguageConfigs/*Textmap.json` files. **Every item that appears in any `PoolConfigs/*.json` file must have a corresponding entry.**

Example entries:

| Key | EN | ZH |
|---|---|---|
| `avatar.Himeko` | Himeko | 姬子 |
| `avatar.Seele` | Seele | 希儿 |
| `avatar.March 7th` | March 7th | 三月七 |
| `lightcone.Adversarial` | Adversarial | 相抗 |
| `lightcone.Night on the Milky Way` | Night on the Milky Way | 银河铁道之夜 |

---

## 3. Code Requirements

### 3.1 New Files

Create two new files under `HSR-Gacha-Simulator/`:

**`LocalizationService.cs`** — Requirements:
- Must be a singleton accessible via `LocalizationService.Instance`.
- Must implement `INotifyPropertyChanged`.
- Must expose:
  - `CurrentLanguage` (`string`, get/set) — changing this switches language and raises `PropertyChanged` for `"Item[]"` (the indexer magic name) so all XAML bindings re-evaluate.
  - `AvailableLanguages` (`IReadOnlyList<string>`, get) — sourced from `meta.languages`.
  - Indexer `this[string key]` — returns the string for `CurrentLanguage`; used by XAML bindings.
  - `Get(string key)` — same as indexer, for code-behind callers.
  - `Get(string key, params object[] args)` — wraps `string.Format(Get(key), args)`.
  - `GetItemName(string englishName)` — tries `avatar.<name>` then `lightcone.<name>`, falls back to `englishName` itself. Callers must not need to know an item's type to display its name.
- Must load `TextMap.json` on first use. If the file is missing or malformed, the service must log the error and fall back to returning the key itself for all lookups (app must not crash).
- Must persist the user's language preference to disk (e.g., a small JSON file under `%LOCALAPPDATA%\HSR-Gacha-Simulator\settings.json`) and restore it on startup.
- Fallback chain: `CurrentLanguage` → `meta.defaultLanguage` → return the raw key. Never throw.

**`LocExtension.cs`** — Requirements:
- A WPF `MarkupExtension` so XAML can use `{local:Loc ui.button.warp_x1}`.
- Must return a live `Binding` to `LocalizationService.Instance[key]` so text updates automatically when the language changes. (Hint: detect the target is a `DependencyProperty` on a `DependencyObject`; in that case return a `Binding`; otherwise return the string directly.)
- Must support both the parameterless constructor (`{local:Loc Key=...}`) and the single-argument constructor (`{local:Loc ...}`).

### 3.2 Modified Files

**`App.xaml`:**
- Already has `xmlns:local="clr-namespace:HSR_Gacha_Simulator"`. Verify it exists; add if missing.

**`MainWindow.xaml`:**
- Replace every hardcoded English string attribute with `{local:Loc <key>}` using the keys from §2.3.
- Strings to replace include: `Window.Title`, all `GroupBox.Header`, all `RadioButton.Content`, all `Button.Content`, all hardcoded `TextBlock.Text` in the pity display and statistics sections, all `GridViewColumn.Header`, and the initial status bar `TextBlock.Text`.
- **Exception:** Do NOT localize the `RadioButton` `GroupName` values (they are internal identifiers, not user-visible).
- **Exception:** Do NOT localize the star characters (`★★★★★`, `★★★★`, `★★★`) — these are universal symbols.
- **Exception:** The `" · "` separator between type/path/element in the result card should use `{local:Loc ui.result.separator}`.

**`MainWindow.xaml.cs`:**
- Replace all hardcoded strings assigned at runtime with `LocalizationService.Instance` lookups. This includes:
  - Status bar messages (`"Ready"`, `"Pulling..."`, `"Pulling ×10..."`, `"Initialisation failed"`, the banner-reset message).
  - Result card type labels (`"Avatar"` / `"Light Cone"`).
  - Result card default text (`"Pull to begin"`).
  - Pull number format string (`$"Pull #{index + 1} of {count}"`).
  - Pity guarantee text (`"✅ Guaranteed"` / `"Not Guaranteed"`).
  - Statistics total-pulls string (`$"Total Pulls: {total}"`).
  - The `PathToLabel()` and `FormatElement()` methods — replace switch-expression string literals with `LocalizationService.Instance[$"path.{path}"]` / `LocalizationService.Instance[$"element.{element}"]`.
  - Reset-confirmation dialog title and message.
- Banner names in the reset handler: the current switch expression produces an English label. These should be replaced with the corresponding `ui.banner.*` key lookups so the reset message and confirmation dialog show the correct language.

**`HistoryItemDisplay.cs`:**
- `FormatPath()` — replace string literals with `LocalizationService.Instance[$"path.{path}"]`.
- `FormatElement()` — replace string literals with `LocalizationService.Instance[$"element.{element}"]`.
- `FromItemData()` — `TypeLabel` assignment must use `ui.history.type.avatar` / `ui.history.type.lightcone_short`.
- `FromItemData()` — `Name` assignment must route through `LocalizationService.Instance.GetItemName(item.Name)` so the history grid shows translated names.
- The `"Preserv."` abbreviation: either localize it with a new key `path.Preservation_short`, or drop the abbreviation and use the full path name (prefer the latter — let the grid column width handle it).

**`HSR-Gacha-Simulator.csproj`:**
- Add `TextMap.json` as content copied to the output directory.

### 3.3 Language Switcher UI

Add a language selector to the UI. The recommended approach:
- Place a `ComboBox` in the status bar (right-aligned).
- Bind its `ItemsSource` to `LocalizationService.Instance.AvailableLanguages`.
- Bind its `SelectedItem` to `LocalizationService.Instance.CurrentLanguage` (two-way).
- Label it so it's self-explanatory regardless of current language (consider an icon or a language-agnostic label).

When the user changes the selection, all `{local:Loc}` bindings must update without a window restart.

### 3.4 Items Not to Touch

- `ItemData.cs`, `GachaSystem.cs`, `DataLoader.cs`, `RarityConverters.cs`, `ElementTypeToBrushConverter.cs` — these contain no user-visible strings and should not need changes.
- `PoolConfigs/*.json` — item data files remain English-only (they are data, not UI).
- The 4 existing `LanguageConfigs/*Textmap.json` files — leave in place as reference.

---

## 4. Generating the Item Entries

The 4 existing textmap files contain the EN→ZH translations for all items, paths, elements, and metadata. They must be merged into the unified `TextMap.json` format.

### 4.1 Mapping Rules

| Existing `text-type` | Unified key pattern |
|---|---|
| `AvatarName` | `avatar.<name-EN>` |
| `LightConeName` | `lightcone.<name-EN>` |
| `PathName` | `path.<name-EN>` |
| `ElementName` | `element.<name-EN>` |
| `MetaInfo` | Derive from `name-EN`. The mapping for the 9 meta entries is given explicitly in §2.3. |

### 4.2 Approach

Write a small migration script (PowerShell, C# console helper, or even manual find/replace) that reads each `*Textmap.json`, transforms entries according to the rules above, and writes them into the `entries` dictionary of `TextMap.json`. Then add all the UI and dialog keys from §2.3 manually (their ZH values are already provided in the tables above).

---

## 5. File Layout After Implementation

```
HSR-Gacha-Simulator/
├── LanguageConfigs/
│   ├── TextMap.json                    ← NEW (loaded at runtime)
│   ├── BlueItemsTextmap.json           ← unchanged (reference)
│   ├── GoldItemsTextmap.json           ← unchanged (reference)
│   ├── PurpleItemsTextmap.json         ← unchanged (reference)
│   └── MetaDataTextmap.json            ← unchanged (reference)
│
├── HSR-Gacha-Simulator/
│   ├── LocalizationService.cs          ← NEW
│   ├── LocExtension.cs                 ← NEW
│   ├── App.xaml                        ← MODIFIED (verify xmlns:local)
│   ├── MainWindow.xaml                 ← MODIFIED
│   ├── MainWindow.xaml.cs              ← MODIFIED
│   ├── HistoryItemDisplay.cs           ← MODIFIED
│   └── HSR-Gacha-Simulator.csproj      ← MODIFIED
```

---

## 6. Acceptance Criteria

1. **Default behavior unchanged:** On first launch (no saved preference), the app appears entirely in English, identical to the current build.
2. **ZH mode:** Switching to Chinese changes every user-visible string — window title, group headers, buttons, pity labels, statistics, result card, history grid headers and cell values, status bar, dialog boxes.
3. **Live switching:** Changing the language updates all visible UI instantly. No restart, no window reload.
4. **Item name translation:** Gacha results and history rows display translated names in ZH mode (e.g., "姬子" not "Himeko").
5. **Persistence:** The user's language choice survives application restart.
6. **Existing files untouched:** `PoolConfigs/*.json`, `DataLoader.cs`, `GachaSystem.cs`, `ItemData.cs`, `RarityConverters.cs`, `ElementTypeToBrushConverter.cs` are not modified.
7. **No crashes on missing data:** If `TextMap.json` is missing or a key is absent, the app falls back gracefully (shows the key or English text, never crashes).
8. **Builds and runs:** `dotnet build` succeeds; the existing 4 `*Textmap.json` files are not included in the build output (only `TextMap.json` is).

---

## 7. Extending to Additional Languages (Future)

Adding a third language (e.g., Japanese) requires only data changes — no C# or XAML edits:

1. Add `"jp"` to `meta.languages` in `TextMap.json`.
2. Add `"jp"` values to every entry in the `entries` dictionary.
3. The language-selector ComboBox picks up the new language automatically from `AvailableLanguages`.

`LocalizationService` must be written to handle any language code present in `meta.languages` without hardcoding "en" or "zh".
