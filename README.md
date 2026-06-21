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
- **Result card** — latest pull displayed with rarity border glow and element coloring
- **10-pull enforcement** — every 10-pull batch guarantees at least one 4★ or better
- **Banner reset** — reset any banner to fresh state (with confirmation dialog)
- **Dark theme** — matches HSR's in-game aesthetic

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

## Tech Stack

- **.NET 10.0** (Windows)
- **WPF** (Windows Presentation Foundation)
- **System.Text.Json** for pool data deserialization
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
├── HSR-Gacha-Simulator/
│   ├── ItemData.cs                       # Data model (enums + ItemData class)
│   ├── GachaSystem.cs                    # Core gacha engine (probability, pity, pulls)
│   ├── DataLoader.cs                     # JSON deserialization
│   ├── MainWindow.xaml                   # UI layout
│   ├── MainWindow.xaml.cs                # UI logic & wiring
│   ├── HistoryItemDisplay.cs             # ListView binding model
│   ├── ElementTypeToBrushConverter.cs    # Element → color converter
│   └── RarityConverters.cs               # Rarity → brush converters
├── Element_Color_Correspondence.md       # Element color reference
└── README.md
```

## Pool Configuration

All gacha pools are defined as JSON files in `PoolConfigs/`. Each file is a JSON array of items:

```json
{
    "type": "Avatar",
    "rarity": "Gold",
    "name": "Cyrene",
    "path": "Memory",
    "element-type": "Ice"
}
```

- **Light Cones** omit `element-type`
- **Blue items** are all Light Cones (19 placeholder items)
- **Event banners with no rate-up purples** (Archer/Saber) have JSON files containing only the gold event item — purple pools are left empty and the system falls back to the full standard purple pool

To customize pools, edit the JSON files and rebuild. The `DataLoader` reads them at runtime from the `PoolConfigs/` directory next to the executable.

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
