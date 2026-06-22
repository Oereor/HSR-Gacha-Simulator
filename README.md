# HSR Gacha Simulator

A **Honkai: Star Rail** warp (gacha) simulator built with WPF on .NET. Simulate pulls across multiple banners with accurate pity mechanics, 50/50 guarantees, soft-pity ramps, and per-banner statistics — no Stellar Jade required.

## Features

- **9 banners** covering the full HSR warp experience:
  - **Ordinary** — standard banner with the full gold pool (avatars + light cones)
  - **Event Avatar** — Cyrene, Phainon, Archer, Saber
  - **Event Light Cone** — Cyrene, Phainon, Archer, Saber signature light cones
- **Accurate probability model**:
  - Gold (5★) base rate 0.6%, soft pity from pull 74, hard pity at pull 90 (avatar) / 80 (light cone)
  - Purple (4★) base rate 5.1%, soft pity from pull 9, guaranteed by pull 10
  - 50/50 for event avatars, 75/25 for event light cones
  - Guarantee carry-over after losing 50/50
  - Purple 50/50 guarantee (mirroring the gold mechanic)
- **Celestial pool** — customizable off-rate gold avatar pool for event banners
- **Per-banner independent state** — pity counters, guarantee flags, and history are tracked separately for each banner
- **Pull history** — scrollable list with rarity-colored stars, element-colored text, and full item details
- **Statistics panel** — total pulls and rarity distribution (5★/4★/3★ counts + rates) per banner
- **Result card** — latest pull displayed with rarity border glow, Path and Element icons flanking the item, and element-colored text
- **10-pull enforcement** — every 10-pull batch guarantees at least one 4★ or better
- **Banner reset** — reset any banner to fresh state (with confirmation dialog)
- **Internationalization** — English and Chinese (简体中文) support with runtime language switching; all UI labels, item names, paths, elements, and dialog text are localized
- **Dark theme** — matches HSR's in-game aesthetic

## Internationalization

The simulator supports English and Chinese, switchable at any time via the language selector in the status bar.

- **Runtime switching** — change language without restarting; all visible UI updates instantly
- **Item name translation** — gacha results and history rows display translated names (e.g., "姬子" for "Himeko" in Chinese)
- **Extensible** — adding a third language requires only data changes in `LanguageConfigs/TextMap.json`; no code edits needed
- The user's language preference is persisted across sessions

See [INTERNATIONALIZATION_LOCALIZATION.md](INTERNATIONALIZATION_LOCALIZATION.md) for the full design and architecture.

## Element Color Coding

| Element | Color |
|---------|-------|
| Physical | Silver |
| Fire | Red |
| Ice | Blue |
| Lightning | Pink |
| Wind | Green |
| Quantum | Deep Purple |
| Imaginary | Yellow |

## Architecture

The presentation layer uses the **MVVM** (Model–View–ViewModel) pattern:

- **Model** — `GachaSystem`, `ItemData`, `DataLoader`, `LocalizationService` (pure logic, no UI dependencies)
- **ViewModel** — `MainViewModel` exposes UI-bound state via `INotifyPropertyChanged`; history is an `ObservableCollection<HistoryItemDisplay>` for incremental updates without full-list rebuilds
- **View** — `MainWindow.xaml` uses WPF data binding, value converters, and `{local:Loc}` markup extensions to render the ViewModel state; the code-behind (`MainWindow.xaml.cs`) is a thin layer of event handlers that delegate to the ViewModel

## Tech Stack

- **.NET 10.0** (Windows)
- **WPF** (Windows Presentation Foundation)
- **System.Text.Json** for pool data and textmap deserialization
- No external dependencies

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10 or later

### Build & Run

```bash
git clone https://github.com/Oereor/HSR-Gacha-Simulator.git
cd HSR-Gacha-Simulator
dotnet build
dotnet run --project HSR-Gacha-Simulator
```

Or open `HSR-Gacha-Simulator.slnx` in Visual Studio / JetBrains Rider and press F5.

## Project Structure

```
├── PoolConfigs/                          # JSON pool data files
│   ├── OrdinaryGoldPoolConfig.json       # Standard 5★ pool (avatars + light cones)
│   ├── OrdinaryPurplePoolConfig.json     # Standard 4★ pool (avatars + light cones)
│   ├── CelestialGoldPoolConfig.json      # Off-rate 5★ avatar pool for event banners
│   ├── BluePoolConfig.json               # 3★ light cone pool
│   ├── CyreneEventAvatarPoolConfig.json  # Cyrene event avatar banner
│   ├── CyreneEventLightConePoolConfig.json
│   ├── PhainonEventAvatarPoolConfig.json
│   ├── PhainonEventLightConePoolConfig.json
│   ├── ArcherEventAvatarPoolConfig.json
│   ├── ArcherEventLightConePoolConfig.json
│   ├── SaberEventAvatarPoolConfig.json
│   └── SaberEventLightConePoolConfig.json
├── LanguageConfigs/                      # Localization data
│   ├── TextMap.json                      # Unified EN/ZH translation file (loaded at runtime)
│   ├── BlueItemsTextmap.json             # Reference: 3★ light cone name translations
│   ├── GoldItemsTextmap.json             # Reference: 5★ item name translations
│   ├── PurpleItemsTextmap.json           # Reference: 4★ item name translations
│   └── MetaDataTextmap.json              # Reference: path/element/metadata translations
├── Icons/                                # Path and Element icon assets (16 PNGs)
│   ├── Path_Destruction.png
│   ├── Path_TheHunt.png
│   ├── Path_Erudition.png
│   ├── Path_Harmony.png
│   ├── Path_Nihility.png
│   ├── Path_Preservation.png
│   ├── Path_Abundance.png
│   ├── Path_Remembrance.png
│   ├── Path_Elation.png
│   ├── Element_Physical.png
│   ├── Element_Fire.png
│   ├── Element_Ice.png
│   ├── Element_Lightning.png
│   ├── Element_Wind.png
│   ├── Element_Quantum.png
│   └── Element_Imaginary.png
├── HSR-Gacha-Simulator/
│   ├── ItemData.cs                       # Data model (enums + ItemData class)
│   ├── GachaSystem.cs                    # Core gacha engine (probability, pity, pulls)
│   ├── DataLoader.cs                     # JSON deserialization
│   ├── LocalizationService.cs            # Localization singleton (TextMap loader, lookup, persistence)
│   ├── LocExtension.cs                   # WPF markup extension for {local:Loc key}
│   ├── MainViewModel.cs                  # MVVM ViewModel — UI state, data binding, navigation
│   ├── IconLoader.cs                     # Cached PNG icon loading for path/element assets
│   ├── MainWindow.xaml                   # UI layout (WPF data binding)
│   ├── MainWindow.xaml.cs                # UI event handlers (thin code-behind)
│   ├── HistoryItemDisplay.cs             # ListView binding model
│   ├── ElementTypeToBrushConverter.cs    # Element → color converter
│   └── RarityConverters.cs               # Rarity → brush converters
├── INTERNATIONALIZATION_LOCALIZATION.md  # i18n/l10n design & implementation spec
├── RESULT_CARD_ICONS.md                  # Result card icon feature spec
└── README.md
```

## Pool Configuration

All gacha pools are defined as JSON files in `PoolConfigs/`. Each file is a JSON array of items:

```json
{
    "type": "Avatar",
    "rarity": "Gold",
    "name": "Cyrene",
    "path": "Remembrance",
    "element-type": "Ice"
}
```

- **Light Cones** omit `element-type`
- **Blue items** are all Light Cones (real 3-star Light Cones)
- **Event banners with no rate-up purples** (Archer/Saber) have JSON files containing only the gold event item — purple pools are left empty and the system falls back to the full standard purple pool

To customize pools, edit the JSON files and rebuild. The `DataLoader` reads them at runtime from the `PoolConfigs/` directory next to the executable.

## Adding a New Language

1. Add the language code (e.g., `"jp"`) to `meta.languages` in `LanguageConfigs/TextMap.json`.
2. Add translations for every key in the `entries` dictionary under the new language code.
3. Rebuild. The language selector picks up the new option automatically.

No C# or XAML changes required. See [INTERNATIONALIZATION_LOCALIZATION.md](INTERNATIONALIZATION_LOCALIZATION.md) §7 for details.

## Mechanics Reference

### Pity System

| Rarity | Base Rate | Soft Pity Start | Hard Pity | Ramp Step |
|--------|-----------|-----------------|-----------|-----------|
| Gold (Avatar banner) | 0.6% | Pull 74 | Pull 90 | +6.0%/pull |
| Gold (LC banner) | 0.6% | Pull 65 | Pull 80 | +7.0%/pull |
| Purple | 5.1% | Pull 9 | Pull 10 | +50.0%/pull |

### Event Guarantee

| Banner type | Win rate-up | Off-rate pool on loss | Guarantee after loss |
|-------------|------------|----------------------|---------------------|
| Event Avatar | 50% | 7 celestial avatars + event avatar | Next gold is guaranteed event |
| Event Light Cone | 75% | Standard gold light cones | Next gold is guaranteed event |
| Ordinary | N/A | Full standard pool | N/A |

## License

MIT
