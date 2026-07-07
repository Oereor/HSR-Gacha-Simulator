# HSR Gacha Simulator — MVVM + DI Refactor Design Document

> **Audience:** Coder agent implementing this refactor.
> **Constraint:** Zero semantic change to user-facing behavior, UI layout, gacha probabilities, or localization strings. This is purely an internal architecture cleanup.

---

## 1. Target Directory Structure

All source files currently live flat in `HSR-Gacha-Simulator/`. After the refactor:

```
HSR-Gacha-Simulator/
├── Models/
│   ├── ItemData.cs                  ← moved, unchanged
│   ├── GachaSystem.cs               ← moved, unchanged (includes GachaType enum)
│   └── EventPoolConfigEntry.cs      ← moved, unchanged
│
├── Services/
│   ├── Interfaces/
│   │   ├── ILocalizationService.cs  ← NEW
│   │   ├── IPoolDataService.cs      ← NEW
│   │   └── IIconService.cs          ← NEW
│   ├── LocalizationService.cs       ← moved, now implements ILocalizationService
│   ├── PoolDataService.cs           ← NEW (wraps former DataLoader static methods)
│   └── IconService.cs               ← NEW (wraps former IconLoader static methods)
│
├── ViewModels/
│   ├── BaseViewModel.cs             ← NEW (INotifyPropertyChanged base)
│   ├── MainViewModel.cs             ← moved, significantly slimmed
│   ├── BannerViewModel.cs           ← extracted from MainViewModel.BannerInfo
│   ├── PityStatisticsViewModel.cs   ← NEW (pity + statistics logic)
│   ├── ResultCardViewModel.cs       ← NEW (result card logic + navigation)
│   ├── HistoryPanelViewModel.cs     ← NEW (history list management)
│   └── HistoryItemDisplay.cs        ← moved, unchanged
│
├── Views/
│   ├── MainWindow.xaml              ← moved, trimmed (resources extracted)
│   └── MainWindow.xaml.cs           ← moved, slimmed (DI constructor injection)
│
├── Converters/
│   ├── RarityConverters.cs          ← moved, unchanged
│   └── ElementTypeToBrushConverter.cs ← moved, unchanged
│
├── Markup/
│   └── LocExtension.cs              ← moved, unchanged
│
├── Resources/
│   ├── Brushes.xaml                 ← NEW (SolidColorBrush definitions)
│   ├── Styles.xaml                  ← NEW (implicit + keyed styles)
│   └── Converters.xaml              ← NEW (converter instance declarations)
│
├── App.xaml                         ← EDITED (merged ResourceDictionary references)
├── App.xaml.cs                      ← EDITED (DI container, manual resolution)
└── AssemblyInfo.cs                  ← moved, unchanged
```

**Files to DELETE after migration:**
- `HSR-Gacha-Simulator/DataLoader.cs` — logic moved into `PoolDataService`
- `HSR-Gacha-Simulator/IconLoader.cs` — logic moved into `IconService`

---

## 2. Interface Definitions

### 2.1 `Services/Interfaces/ILocalizationService.cs`

```csharp
namespace HSR_Gacha_Simulator.Services;

public interface ILocalizationService : INotifyPropertyChanged
{
    string CurrentLanguage { get; set; }
    IReadOnlyList<string> AvailableLanguages { get; }

    string this[string key] { get; }
    string Get(string key);
    string Get(string key, params object[] args);
    string GetItemName(string englishName);
}
```

Every public member of the existing `LocalizationService` becomes an interface member. The singleton pattern (`static Instance`) is **removed** — the DI container manages lifetime as a single-instance registration.

### 2.2 `Services/Interfaces/IPoolDataService.cs`

```csharp
namespace HSR_Gacha_Simulator.Services;

public interface IPoolDataService
{
    /// <summary>Load a single pool-config JSON file from disk.</summary>
    List<ItemData> LoadFromFile(string filePath);

    /// <summary>
    /// Build a two-level lookup keyed by (ItemType, ItemRarity) → item name → full ItemData.
    /// Used as the master reference for enriching event-banner items.
    /// </summary>
    Dictionary<(ItemType, ItemRarity), Dictionary<string, ItemData>>
        BuildMasterLookup(string allGoldPath, string ordinaryPurplePath);

    /// <summary>
    /// Load the consolidated EventPoolConfigs.json, filter to enabled entries,
    /// and enrich each simplified item with full data from the master lookup.
    /// </summary>
    List<EventPoolConfigEntry> LoadEventPoolConfigs(
        string filePath,
        Dictionary<(ItemType, ItemRarity), Dictionary<string, ItemData>> masterLookup);
}
```

All three methods are instance methods on `PoolDataService`. The DTO classes (`ItemDataDto`, `EventPoolEntryDto`) and private helpers (`ToItemData`, `ParseEnum<T>`, `LogWarning`) stay private inside `PoolDataService`.

### 2.3 `Services/Interfaces/IIconService.cs`

```csharp
namespace HSR_Gacha_Simulator.Services;

public interface IIconService
{
    /// <summary>
    /// Returns a cached BitmapImage for the given file name inside the Icons/ directory,
    /// or null if the file does not exist.
    /// </summary>
    BitmapImage? LoadOrNull(string fileName);
}
```

---

## 3. Service Implementations

### 3.1 `Services/LocalizationService.cs`

**What changes from the current file:**
- Add `: ILocalizationService` to the class declaration.
- **Remove** the static singleton machinery: delete `_instance`, `Instance` property, the `Lazy<LocalizationService>` field.
- Make the constructor **public** (it is currently `private`). The DI container calls it.
- All existing method bodies, the indexer, `EnsureLoaded()`, settings persistence, and error logging remain **unchanged**.
- The `INotifyPropertyChanged` implementation stays as-is.

**DI lifetime:** Singleton (one instance for the application lifetime).

### 3.2 `Services/PoolDataService.cs`

**What this is:** The instance-method equivalent of the current static `DataLoader` class.

- Implements `IPoolDataService`.
- Copy all three public method bodies from `DataLoader` verbatim — no logic changes.
- Copy all private helpers (`ToItemData`, `ParseEnum<T>`, `LogWarning`) and both private DTO classes (`ItemDataDto`, `EventPoolEntryDto`) verbatim.
- No state — all methods are pure I/O.

**DI lifetime:** Singleton (stateless, one instance is fine).

### 3.3 `Services/IconService.cs`

**What this is:** The instance-method equivalent of the current static `IconLoader` class.

- Implements `IIconService`.
- Copy `LoadOrNull` method body, the `Cache` dictionary, and the `IconsDir` field verbatim from `IconLoader`.
- No static members except the cache dictionary which becomes an instance field.

**DI lifetime:** Singleton (the `BitmapImage` cache should persist across calls).

---

## 4. ViewModel Architecture

### 4.1 `ViewModels/BaseViewModel.cs`

Extract the `INotifyPropertyChanged` boilerplate into a base class used by all ViewModels.

```csharp
namespace HSR_Gacha_Simulator.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets the backing field and raises PropertyChanged if the value differs.
    /// Returns true if the value changed.
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
```

All existing manual property implementations (e.g. `_goldPity` / `GoldPity`) should use `SetProperty` instead of manual `OnPropertyChanged()` calls after assignment. Example migration:

```csharp
// Before:
private string _goldPity = "0";
public string GoldPity { get => _goldPity; set { _goldPity = value; OnPropertyChanged(); } }

// After:
private string _goldPity = "0";
public string GoldPity { get => _goldPity; set => SetProperty(ref _goldPity, value); }
```

### 4.2 `ViewModels/BannerViewModel.cs`

**Source:** Extracted from the `BannerInfo` class currently at lines 740–773 of `MainViewModel.cs`.

- Rename class from `BannerInfo` to `BannerViewModel`.
- Extend `BaseViewModel` instead of implementing `INotifyPropertyChanged` directly.
- `IsSelected` setter uses `SetProperty`.
- All `OnPropertyChanged()` calls removed (use `SetProperty` in `IsSelected` only; other properties are set once at construction).
- Remove the `INotifyPropertyChanged` implementation boilerplate — inherited from `BaseViewModel`.
- Constructor takes `(GachaSystem system, string bannerKey, string bannerTitle, GachaType gachaType)` and sets all properties. This replaces the current pattern of `new BannerInfo { System = ..., BannerKey = ..., ... }`.

**DI lifetime:** Transient (created per banner in the initialization loop).

### 4.3 `ViewModels/HistoryItemDisplay.cs`

**Changes:**
- Extend `BaseViewModel` instead of implementing `INotifyPropertyChanged` directly.
- Replace all manual `OnPropertyChanged()` calls with `SetProperty`.
- Remove the `INotifyPropertyChanged` boilerplate — inherited.
- The static factory method `FromItemData` and static formatters stay unchanged.

### 4.4 `ViewModels/PityStatisticsViewModel.cs`

**Extracted from `MainViewModel`:** Pity display properties + statistics properties + their refresh methods.

**Properties (all with `SetProperty` via `BaseViewModel`):**

| CLR Property | Type | Source (MainViewModel lines) |
|---|---|---|
| `GoldPity` | `string` | 78–82 |
| `GoldGuarantee` | `string` | 85–89 |
| `GoldGuaranteeBrush` | `Brush` | 91–97 |
| `PurplePity` | `string` | 100–104 |
| `PurpleGuarantee` | `string` | 107–111 |
| `PurpleGuaranteeBrush` | `Brush` | 113–118 |
| `TotalPulls` | `string` | 121–126 |
| `GoldCount` | `string` | 128–133 |
| `GoldRate` | `string` | 135–140 |
| `PurpleCount` | `string` | 142–148 |
| `PurpleRate` | `string` | 149–154 |
| `BlueCount` | `string` | 156–161 |
| `BlueRate` | `string` | 163–168 |
| `MissedGoldStats` | `string` | 176–180 |
| `MissedStatsVisibility` | `Visibility` | 185–189 |

**Methods:**

```
void Refresh(GachaSystem system)
```

This method takes a `GachaSystem` and updates all the properties above. Its body is the merged logic of:
1. The existing `RefreshPity()` method (MainViewModel lines 560–575), except `CurrentSystem` is replaced by the `system` parameter.
2. The existing `RefreshStatistics()` method (MainViewModel lines 581–619), same parameter replacement.

The cached brushes (`GoldBrush`, `DimBrush`) move from `MainViewModel` into this class as private static fields.

**Constructor:** Takes `ILocalizationService` (injected) so it can look up localized strings for pity/guarantee text and stats formatting.

### 4.5 `ViewModels/ResultCardViewModel.cs`

**Extracted from `MainViewModel`:** Result card display properties + ShowResult/ClearResult + navigation.

**Properties (all with `SetProperty` via `BaseViewModel`):**

| CLR Property | Type | Source (MainViewModel lines) |
|---|---|---|
| `ResultRarity` | `string` | 192–197 |
| `ResultRarityBrush` | `Brush` | 199–204 |
| `ResultName` | `string` | 206–211 |
| `ResultType` | `string` | 213–218 |
| `ResultPath` | `string` | 220–225 |
| `ResultElement` | `string` | 227–232 |
| `ResultElementBrush` | `Brush` | 234–239 |
| `ResultPathIcon` | `ImageSource?` | 241–246 |
| `ResultPathIconVisibility` | `Visibility` | 248–253 |
| `ResultElementIcon` | `ImageSource?` | 255–260 |
| `ResultElementIconVisibility` | `Visibility` | 262–267 |
| `ResultCardBorderBrush` | `Brush` | 269–274 |
| `ResultIndexText` | `string` | 276–281 |
| `DotElementVisibility` | `Visibility` | 283–288 |
| `ResultElementTextVisibility` | `Visibility` | 290–295 |

**Methods:**

```
void ShowResult(GachaSystem system, int index)
```
Body is the existing `ShowResult(int index)` (lines 625–687), with these changes:
- `CurrentSystem` → `system` parameter
- The `CurrentResultIndex` set is removed (that property lives on `HistoryPanelViewModel`)
- `RarityConverters.GetForegroundBrush(...)` call stays (static class, no DI needed)
- `ElementTypeToBrushConverter.GetBrush(...)` call stays
- `IconLoader.LoadOrNull(...)` → uses injected `IIconService.LoadOrNull(...)`

```
void ClearResult()
```
Body is the existing `ClearResult()` (lines 689–704), unchanged except `DefaultCardBorderBrush` becomes a private static field of this class.

```
int NavigatePrev(int currentIndex, int historyCount)
```
Returns the new index (wraps to `historyCount - 1` if going before 0, returns `currentIndex` unchanged if history is empty). Pure logic — does not set any property. The calling code in `MainViewModel` sets `CurrentResultIndex` on `HistoryPanelViewModel`.

```
int NavigateNext(int currentIndex, int historyCount)
```
Same pattern — returns the new index or wraps to 0.

**Constructor:** Takes `ILocalizationService` and `IIconService` (injected).

### 4.6 `ViewModels/HistoryPanelViewModel.cs`

**Extracted from `MainViewModel`:** History collection management + async reload + current result index.

**Properties:**

| CLR Property | Type | Source |
|---|---|---|
| `HistoryItems` | `ObservableCollection<HistoryItemDisplay>` | MainViewModel line 46 |
| `CurrentResultIndex` | `int` (default -1) | MainViewModel lines 48–53 |

Both use `SetProperty` via `BaseViewModel`.

**Methods:**

```
void AppendNewHistoryItems(GachaSystem system, int count)
```
Body from `MainViewModel.AppendNewHistoryItems(int count)` (lines 489–497), but `CurrentSystem` → `system` parameter.

```
void ClearHistory()
```
Body from `MainViewModel.ClearHistory()` (lines 500–503), unchanged.

```
async Task ReloadAllHistoryAsync(GachaSystem system)
```
Body from `MainViewModel.ReloadAllHistoryAsync()` (lines 513–554), but:
- `CurrentSystem` → `system` parameter
- `IsLoading` set/clear and `StatusText` are removed — those are on `MainViewModel` and will be set by `MainViewModel` before/after calling this method
- The `catch`/`finally` blocks that set `IsLoading`/`StatusText` are removed from this method

**Constructor:** Takes `ILocalizationService` (injected) — needed for the `HistoryItemDisplay.FromItemData` calls which use localization.

### 4.7 `ViewModels/MainViewModel.cs`

**After extraction, MainViewModel retains only:**
- Banner list management (`Banners`, `SelectedBanner`, `CurrentSystem`, `CurrentBannerKey`)
- Loading/status state (`IsLoading`, `StatusText`)
- `InitializeSystems()` (delegates to services)
- `Pull(int count)` and `ResetCurrentBanner()`
- `AfterPull(int count)` (orchestration — calls sub-VMs)
- `ResetBanner(string bannerName)`
- `OnBannerSwitched(BannerViewModel?)`

**Properties retained:**

| CLR Property | Type |
|---|---|
| `Banners` | `ObservableCollection<BannerViewModel>` |
| `SelectedBanner` | `BannerViewModel?` |
| `IsLoading` | `bool` |
| `StatusText` | `string` |
| `CurrentSystem` | `GachaSystem` (computed from SelectedBanner) |
| `CurrentBannerKey` | `string` (computed from SelectedBanner) |

Plus **child ViewModel properties** (read-only, created in constructor):

| Property | Type |
|---|---|
| `PityStats` | `PityStatisticsViewModel` |
| `ResultCard` | `ResultCardViewModel` |
| `HistoryPanel` | `HistoryPanelViewModel` |

**Constructor:**

```csharp
public MainViewModel(
    ILocalizationService localizationService,
    IPoolDataService poolDataService,
    IIconService iconService)
```

Injected dependencies are forwarded to sub-ViewModels:
- `_pityStats = new PityStatisticsViewModel(localizationService)`
- `_resultCard = new ResultCardViewModel(localizationService, iconService)`
- `_historyPanel = new HistoryPanelViewModel(localizationService)`

The `InitializeSystems()` method now receives `IPoolDataService` as a field `_poolDataService` and uses it instead of the static `DataLoader` calls.

**Method changes in `InitializeSystems()`:**
- `DataLoader.LoadFromFile(...)` → `_poolDataService.LoadFromFile(...)` (6 call sites)
- `DataLoader.BuildMasterLookup(...)` → `_poolDataService.BuildMasterLookup(...)` (1 call site)
- `DataLoader.LoadEventPoolConfigs(...)` → `_poolDataService.LoadEventPoolConfigs(...)` (1 call site)
- `new BannerInfo { ... }` → `new BannerViewModel(system, key, title, type)` (all banner creation sites)
- `BannerInfo` type references → `BannerViewModel`
- `_selectedBanner?.IsSelected = false` / `true` stays (BannerViewModel has the property)

**Method `Pull(int count)`:**
```csharp
public void Pull(int count)
{
    var sys = CurrentSystem;
    if (count == 1)
        sys.Pull();
    else
        sys.Pull10();

    _historyPanel.CurrentResultIndex = sys.History.Count - 1;
    AfterPull(count);
}
```

**Method `AfterPull(int count)`:**
```csharp
public void AfterPull(int count)
{
    var sys = CurrentSystem;
    _historyPanel.AppendNewHistoryItems(sys, count);
    _pityStats.Refresh(sys);
    _resultCard.ShowResult(sys, _historyPanel.CurrentResultIndex);
    StatusText = _localizationService.Get("ui.status.ready");
}
```

**Methods `NavigatePrev()` / `NavigateNext()`:**
```csharp
public void NavigatePrev()
{
    var sys = CurrentSystem;
    if (sys.History.Count == 0) return;
    int newIdx = _resultCard.NavigatePrev(_historyPanel.CurrentResultIndex, sys.History.Count);
    _historyPanel.CurrentResultIndex = newIdx;
    _resultCard.ShowResult(sys, newIdx);
}
// NavigateNext is analogous using _resultCard.NavigateNext(...)
```

**Method `ResetBanner(string bannerName)`:**
```csharp
public void ResetBanner(string bannerName)
{
    _historyPanel.CurrentResultIndex = -1;
    _resultCard.ClearResult();
    _historyPanel.ClearHistory();
    _pityStats.Refresh(CurrentSystem);
    StatusText = _localizationService.Get("ui.status.banner_reset", bannerName);
}
```

**Method `OnBannerSwitched(BannerViewModel? banner)`:**
```csharp
private void OnBannerSwitched(BannerViewModel? banner)
{
    if (banner == null) return;
    _historyPanel.CurrentResultIndex = -1;
    _resultCard.ClearResult();
    _ = _historyPanel.ReloadAllHistoryAsync(CurrentSystem);
    _pityStats.Refresh(CurrentSystem);
    StatusText = _localizationService.Get("ui.status.ready");
}
```

**XAML binding paths change — all previous direct bindings on MainViewModel properties that moved to sub-VMs must be prefixed:**

| Old Binding Path | New Binding Path |
|---|---|
| `GoldPity` | `PityStats.GoldPity` |
| `GoldGuarantee` | `PityStats.GoldGuarantee` |
| `GoldGuaranteeBrush` | `PityStats.GoldGuaranteeBrush` |
| `PurplePity` | `PityStats.PurplePity` |
| `PurpleGuarantee` | `PityStats.PurpleGuarantee` |
| `PurpleGuaranteeBrush` | `PityStats.PurpleGuaranteeBrush` |
| `TotalPulls` | `PityStats.TotalPulls` |
| `GoldCount` | `PityStats.GoldCount` |
| `GoldRate` | `PityStats.GoldRate` |
| `PurpleCount` | `PityStats.PurpleCount` |
| `PurpleRate` | `PityStats.PurpleRate` |
| `BlueCount` | `PityStats.BlueCount` |
| `BlueRate` | `PityStats.BlueRate` |
| `MissedGoldStats` | `PityStats.MissedGoldStats` |
| `MissedStatsVisibility` | `PityStats.MissedStatsVisibility` |
| `ResultRarity` | `ResultCard.ResultRarity` |
| `ResultRarityBrush` | `ResultCard.ResultRarityBrush` |
| `ResultName` | `ResultCard.ResultName` |
| `ResultType` | `ResultCard.ResultType` |
| `ResultPath` | `ResultCard.ResultPath` |
| `ResultElement` | `ResultCard.ResultElement` |
| `ResultElementBrush` | `ResultCard.ResultElementBrush` |
| `ResultPathIcon` | `ResultCard.ResultPathIcon` |
| `ResultPathIconVisibility` | `ResultCard.ResultPathIconVisibility` |
| `ResultElementIcon` | `ResultCard.ResultElementIcon` |
| `ResultElementIconVisibility` | `ResultCard.ResultElementIconVisibility` |
| `ResultCardBorderBrush` | `ResultCard.ResultCardBorderBrush` |
| `ResultIndexText` | `ResultCard.ResultIndexText` |
| `DotElementVisibility` | `ResultCard.DotElementVisibility` |
| `ResultElementTextVisibility` | `ResultCard.ResultElementTextVisibility` |
| `HistoryItems` | `HistoryPanel.HistoryItems` |
| `CurrentResultIndex` | `HistoryPanel.CurrentResultIndex` |

Properties that remain on `MainViewModel` directly (no prefix change):
- `Banners`, `SelectedBanner`, `IsLoading`, `StatusText`, `CurrentBannerKey`

---

## 5. View Changes

### 5.1 `Views/MainWindow.xaml`

**Changes from the current `MainWindow.xaml`:**

1. **Remove inline resources** (currently lines 15–266, the entire `<Window.Resources>` block) and replace with merged resource dictionaries:
   ```xml
   <Window.Resources>
       <ResourceDictionary>
           <ResourceDictionary.MergedDictionaries>
               <ResourceDictionary Source="/Resources/Brushes.xaml"/>
               <ResourceDictionary Source="/Resources/Styles.xaml"/>
               <ResourceDictionary Source="/Resources/Converters.xaml"/>
           </ResourceDictionary.MergedDictionaries>
       </ResourceDictionary>
   </Window.Resources>
   ```

2. **Update all Binding paths** according to the table in §4.7 above. Nothing else in the XAML layout changes — no control positions, sizes, colors, or templates.

3. **Namespace declarations**: Add `xmlns:vm="clr-namespace:HSR_Gacha_Simulator.ViewModels"` if needed (unlikely since all bindings are on the DataContext which is still `MainViewModel`).

### 5.2 `Views/MainWindow.xaml.cs`

**Changes from the current `MainWindow.xaml.cs`:**

1. **Constructor injection:**
   ```csharp
   public partial class MainWindow : Window
   {
       private readonly MainViewModel _viewModel;

       public MainWindow(MainViewModel viewModel)
       {
           _viewModel = viewModel;
           InitializeComponent();
           DataContext = _viewModel;
           lvHistory.ItemsSource = _viewModel.HistoryPanel.HistoryItems;

           try
           {
               _viewModel.InitializeSystems();
           }
           catch (Exception ex)
           {
               MessageBox.Show(
                   _viewModel.StatusText,  // will be set by catch block below
                   "Error",  // or use L10n from injected service
                   MessageBoxButton.OK, MessageBoxImage.Error);
           }
       }
   }
   ```

   Wait — `StatusText` is not yet set in the catch. Keep the original pattern but inject `ILocalizationService`:
   ```csharp
   private readonly MainViewModel _viewModel;
   private readonly ILocalizationService _l10n;

   public MainWindow(MainViewModel viewModel, ILocalizationService localizationService)
   {
       _viewModel = viewModel;
       _l10n = localizationService;
       InitializeComponent();
       DataContext = _viewModel;
       lvHistory.ItemsSource = _viewModel.HistoryPanel.HistoryItems;

       try
       {
           _viewModel.InitializeSystems();
       }
       catch (Exception ex)
       {
           MessageBox.Show(
               _l10n.Get("dialog.error.init_failed", ex.Message),
               _l10n.Get("dialog.error.title"),
               MessageBoxButton.OK, MessageBoxImage.Error);
           _viewModel.StatusText = _l10n.Get("ui.status.init_failed");
       }
   }
   ```

2. **Remove** the `initialising` field and guard — `InitializeSystems()` is called in the constructor before the window is shown, and no event handlers fire during construction.

3. **Remove** the `private static LocalizationService L10n` shorthand — use the injected `_l10n` field instead.

4. **Event handler changes:**
   - All event handlers stay but use `_viewModel` instead of `viewModel` and `_l10n` instead of `L10n`.
   - `lvHistory.ItemsSource` binding moves to constructor (already there, but uses `_viewModel.HistoryPanel.HistoryItems`).

---

## 6. Resource Extraction

### 6.1 `Resources/Brushes.xaml`

Extract all `SolidColorBrush` definitions currently in `MainWindow.xaml` lines 17–31 (approximate) into a standalone `ResourceDictionary`. Each brush keeps the same `x:Key`. The brushes are:

| x:Key | Color |
|---|---|
| `BgDarkBrush` | `#1a1a2e` |
| `BgCardBrush` | `#16213e` |
| `BtnBgBrush` | existing value |
| `TextPrimaryBrush` | existing value |
| `TextSecondaryBrush` | existing value |
| `TextDimBrush` | existing value |
| `GoldAccentBrush` | existing value |
| `PurpleAccentBrush` | existing value |
| `BlueAccentBrush` | existing value |
| `StatusBarBrush` | `#0f3460` |

### 6.2 `Resources/Styles.xaml`

Extract all `<Style>` elements from `MainWindow.xaml`'s `<Window.Resources>` into this file. The styles are:
- `GroupBox` (implicit)
- `WarpButtonStyle` (x:Key)
- `NavButtonStyle` (x:Key)
- `ListView` (implicit)
- `ListViewItem` (implicit)
- `StatusBar` (implicit)
- `PityTextStyle` (x:Key)
- `ResetButtonStyle` (x:Key)
- `ComboBoxItem` (implicit)
- `ComboBox` (implicit)

### 6.3 `Resources/Converters.xaml`

Declare converter instances that were previously in `MainWindow.xaml`'s resources:
```xml
<ResourceDictionary xmlns="..."
                    xmlns:local="clr-namespace:HSR_Gacha_Simulator.Converters">
    <local:ElementTypeToBrushConverter x:Key="ElementBrushConverter"/>
    <local:RarityToBorderBrushConverter x:Key="RarityBorderConverter"/>
    <local:RarityToForegroundConverter x:Key="RarityForegroundConverter"/>
</ResourceDictionary>
```

---

## 7. DI Container Configuration

### 7.1 Add NuGet Package

Add `Microsoft.Extensions.DependencyInjection` to `HSR-Gacha-Simulator.csproj`. This is the only new dependency.

```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />
```

### 7.2 `App.xaml.cs`

Replace the current empty `App` class with a DI bootstrapper:

```csharp
using Microsoft.Extensions.DependencyInjection;
using HSR_Gacha_Simulator.Services;
using HSR_Gacha_Simulator.ViewModels;
using HSR_Gacha_Simulator.Views;

namespace HSR_Gacha_Simulator;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();

        // Services — singleton
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IPoolDataService, PoolDataService>();
        services.AddSingleton<IIconService, IconService>();

        // ViewModels — transient (new instance each resolution; MainViewModel is effectively singleton via window lifetime)
        services.AddTransient<MainViewModel>();
        // Sub-ViewModels are created manually by MainViewModel, not resolved from DI.
        // If you prefer sub-VMs in DI, register them as Transient and have MainViewModel accept them.

        // Views
        services.AddSingleton<MainWindow>();  // one window for app lifetime

        _serviceProvider = services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
```

**Important:** Remove `StartupUri="MainWindow.xaml"` from `App.xaml`. The window is now created and shown in `OnStartup` via DI.

### 7.3 `App.xaml`

```xml
<Application x:Class="HSR_Gacha_Simulator.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="/Resources/Brushes.xaml"/>
                <ResourceDictionary Source="/Resources/Styles.xaml"/>
                <ResourceDictionary Source="/Resources/Converters.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

- `StartupUri` attribute is **removed**.
- The merged dictionaries that were in `MainWindow.xaml` are now here in `App.xaml` so all converters and styles are available application-wide. Remove the merged dictionaries from `MainWindow.xaml` — its `<Window.Resources>` becomes empty.

---

## 8. Namespace Changes

| Old namespace (flat) | New namespace |
|---|---|
| `HSR_Gacha_Simulator` (all files) | `HSR_Gacha_Simulator.Models` — ItemData.cs, GachaSystem.cs, EventPoolConfigEntry.cs |
| | `HSR_Gacha_Simulator.Services` — LocalizationService.cs, PoolDataService.cs, IconService.cs, Interfaces/* |
| | `HSR_Gacha_Simulator.ViewModels` — All ViewModels + HistoryItemDisplay.cs |
| | `HSR_Gacha_Simulator.Views` — MainWindow.xaml, MainWindow.xaml.cs |
| | `HSR_Gacha_Simulator.Converters` — RarityConverters.cs, ElementTypeToBrushConverter.cs |
| | `HSR_Gacha_Simulator.Markup` — LocExtension.cs |
| | `HSR_Gacha_Simulator` — App.xaml, App.xaml.cs, AssemblyInfo.cs (root namespace) |

Every moved file must have its `namespace` declaration updated to match its new folder.

Every file that references a moved type must add the appropriate `using` directive. Specifically:

- `GachaSystem.cs` references `ItemData`, `ItemType`, `ItemRarity` → add `using HSR_Gacha_Simulator.Models;` (or they're in the same namespace if both in `Models/`)
- `PoolDataService.cs` references `ItemData`, `EventPoolConfigEntry`, enums → add `using HSR_Gacha_Simulator.Models;`
- `MainViewModel.cs` references models and services → add `using HSR_Gacha_Simulator.Models;` and `using HSR_Gacha_Simulator.Services;`
- All ViewModels reference models and services → same
- `MainWindow.xaml` references ViewModels and Converters → update `xmlns:local` to point to the correct namespace; add `xmlns:converters="clr-namespace:HSR_Gacha_Simulator.Converters"` and `xmlns:markup="clr-namespace:HSR_Gacha_Simulator.Markup"` as needed

**XAML namespace changes in `MainWindow.xaml`:**

The current `xmlns:local="clr-namespace:HSR_Gacha_Simulator"` was used for:
1. `{local:Loc ...}` markup extension → now `xmlns:markup="clr-namespace:HSR_Gacha_Simulator.Markup"`, usage becomes `{markup:Loc ...}`
2. Converter classes in resources → now in `xmlns:converters="clr-namespace:HSR_Gacha_Simulator.Converters"`

Update ALL `{local:Loc ...}` to `{markup:Loc ...}` throughout the XAML file.

---

## 9. `.csproj` Changes

1. Add `Microsoft.Extensions.DependencyInjection` PackageReference (see §7.1).
2. Update all `<Content Include="...">` paths — since source files move but the linked config/image files stay at their original paths relative to the project root, these should NOT need changes (they're linked from `../PoolConfigs/`, `../LanguageConfigs/`, `../Icons/`).
3. Ensure `App.xaml` no longer has a `StartupUri` — the attribute is removed in the XAML file itself.

---

## 10. Implementation Order

Follow this order strictly — each step should compile before proceeding to the next:

### Phase 1: Add DI infrastructure
1. Add `Microsoft.Extensions.DependencyInjection` NuGet package to `.csproj`.
2. Create the directory structure (all empty folders).
3. Create `BaseViewModel.cs` in `ViewModels/`.
4. Verify the project compiles.

### Phase 2: Extract services with interfaces
5. Create `Services/Interfaces/ILocalizationService.cs`.
6. Move `LocalizationService.cs` → `Services/LocalizationService.cs`.
   - Change namespace to `HSR_Gacha_Simulator.Services`.
   - Add `: ILocalizationService`.
   - Make constructor `public`.
   - Remove `static Instance` / `Lazy<LocalizationService>` singleton machinery.
7. Create `Services/Interfaces/IPoolDataService.cs`.
8. Create `Services/PoolDataService.cs` — copy logic from `DataLoader.cs`.
9. Create `Services/Interfaces/IIconService.cs`.
10. Create `Services/IconService.cs` — copy logic from `IconLoader.cs`.
11. Verify the project compiles.

### Phase 3: Move models
12. Move `ItemData.cs` → `Models/ItemData.cs`. Update namespace to `HSR_Gacha_Simulator.Models`.
13. Move `GachaSystem.cs` → `Models/GachaSystem.cs`. Update namespace. Add `using HSR_Gacha_Simulator.Models;` for `ItemData` references (or none if same namespace).
14. Move `EventPoolConfigEntry.cs` → `Models/EventPoolConfigEntry.cs`. Update namespace. Add `using HSR_Gacha_Simulator.Models;` for `ItemData`.
15. Update all files that reference moved types with appropriate `using` directives.
16. Verify the project compiles.

### Phase 4: Split ViewModels
17. Move `HistoryItemDisplay.cs` → `ViewModels/HistoryItemDisplay.cs`. Update namespace. Change to extend `BaseViewModel`.
18. Create `ViewModels/BannerViewModel.cs` — extract `BannerInfo` from `MainViewModel.cs`.
19. Create `ViewModels/PityStatisticsViewModel.cs` — extract pity + stats logic.
20. Create `ViewModels/ResultCardViewModel.cs` — extract result card + navigation logic.
21. Create `ViewModels/HistoryPanelViewModel.cs` — extract history management logic.
22. Rewrite `MainViewModel.cs` → `ViewModels/MainViewModel.cs` — slim orchestrator with injected services and sub-VMs.
23. Verify the project compiles.

### Phase 5: Move converters, markup, and views
24. Move `RarityConverters.cs` → `Converters/RarityConverters.cs`. Update namespace.
25. Move `ElementTypeToBrushConverter.cs` → `Converters/ElementTypeToBrushConverter.cs`. Update namespace.
26. Move `LocExtension.cs` → `Markup/LocExtension.cs`. Update namespace.
27. Create resource dictionary files: `Resources/Brushes.xaml`, `Resources/Styles.xaml`, `Resources/Converters.xaml`.
28. Update `MainWindow.xaml` → `Views/MainWindow.xaml`:
   - Remove inline resources.
   - Update all binding paths (see table in §4.7).
   - Change `{local:Loc ...}` → `{markup:Loc ...}`.
   - Update converter references.
29. Update `MainWindow.xaml.cs` → `Views/MainWindow.xaml.cs` — DI constructor injection.
30. Update `App.xaml` — remove `StartupUri`, add merged resource dictionaries.
31. Rewrite `App.xaml.cs` — DI container setup, manual window resolution in `OnStartup`.
32. Move `AssemblyInfo.cs` (unchanged).
33. Verify the project compiles.

### Phase 6: Clean up
34. Delete `DataLoader.cs` (logic moved to `PoolDataService`).
35. Delete `IconLoader.cs` (logic moved to `IconService`).
36. Update `.csproj` if any file references need adjustment.
37. Full build + run. Verify all UI elements render identically, all gacha mechanics produce identical results, language switching works, banner switching works.

---

## 11. Files That Must NOT Change Semantically

The following logic is **protected** and must remain byte-for-byte identical in behavior:

1. **`GachaSystem.cs`** — All probability constants, the `DoGacha` method, `Pull()`, `Pull10()`, `GetGoldItem()`, `GetPurpleItem()`, `GetBlueItem()`, all pity counter logic, all guarantee flag logic. The file moves to `Models/` and the namespace changes, but every line of code stays identical.

2. **`ItemData.cs`** — All enums and the `ItemData` class. Move only, no code changes.

3. **`EventPoolConfigEntry.cs`** — Move only, no code changes.

4. **`HistoryItemDisplay.cs`** — The `FromItemData` factory method and all static formatters stay identical. The only change is extending `BaseViewModel` instead of implementing `INotifyPropertyChanged` directly (which has identical behavior).

5. **`LocalizationService.cs`** — All parsing, fallback, persistence, and retrieval logic stays identical. Only the singleton pattern is replaced by DI (constructor becomes public, `Instance` removed).

6. **`LocExtension.cs`** — Move only, no code changes (except namespace). The `ProvideValue` logic must work identically.

7. **`RarityConverters.cs`** and **`ElementTypeToBrushConverter.cs`** — Move only, no code changes.

8. **All JSON config files** in `PoolConfigs/` and `LanguageConfigs/` — Not touched.

9. **All PNG icon files** in `Icons/` — Not touched.

10. **All localization keys and strings** in `TextMap.json` — Not touched.

---

## 12. XAML Binding Path Reference (Complete)

Every `{Binding}` path in `MainWindow.xaml` that currently binds directly to a `MainViewModel` property must be updated if the property moved. Here is the exhaustive list:

```
Banners                          → (no change)
SelectedBanner                   → (no change)
IsLoading                        → (no change)
StatusText                       → (no change)

GoldPity                         → PityStats.GoldPity
GoldGuarantee                    → PityStats.GoldGuarantee
GoldGuaranteeBrush               → PityStats.GoldGuaranteeBrush
PurplePity                       → PityStats.PurplePity
PurpleGuarantee                  → PityStats.PurpleGuarantee
PurpleGuaranteeBrush             → PityStats.PurpleGuaranteeBrush

TotalPulls                       → PityStats.TotalPulls
GoldCount                        → PityStats.GoldCount
GoldRate                         → PityStats.GoldRate
PurpleCount                      → PityStats.PurpleCount
PurpleRate                       → PityStats.PurpleRate
BlueCount                        → PityStats.BlueCount
BlueRate                         → PityStats.BlueRate
MissedGoldStats                  → PityStats.MissedGoldStats
MissedStatsVisibility            → PityStats.MissedStatsVisibility

ResultRarity                     → ResultCard.ResultRarity
ResultRarityBrush                → ResultCard.ResultRarityBrush
ResultName                       → ResultCard.ResultName
ResultType                       → ResultCard.ResultType
ResultPath                       → ResultCard.ResultPath
ResultElement                    → ResultCard.ResultElement
ResultElementBrush               → ResultCard.ResultElementBrush
ResultPathIcon                   → ResultCard.ResultPathIcon
ResultPathIconVisibility         → ResultCard.ResultPathIconVisibility
ResultElementIcon                → ResultCard.ResultElementIcon
ResultElementIconVisibility      → ResultCard.ResultElementIconVisibility
ResultCardBorderBrush            → ResultCard.ResultCardBorderBrush
ResultIndexText                  → ResultCard.ResultIndexText
DotElementVisibility             → ResultCard.DotElementVisibility
ResultElementTextVisibility      → ResultCard.ResultElementTextVisibility

HistoryItems                     → HistoryPanel.HistoryItems
CurrentResultIndex               → HistoryPanel.CurrentResultIndex
```

The `BannerInfo` type in XAML (e.g., `{Binding DisplayName}` inside the ListBox ItemTemplate) changes to reference `BannerViewModel` because that's what the `Banners` collection now contains. Since `BannerViewModel` has the same property names (`DisplayName`, `IsSelected`, `BannerKey`, `BannerTitle`), no binding path changes are needed inside the banner strip template — only the class name in any `x:Type` or `DataTemplate` references.

---

## 13. Verification Checklist

After implementation, verify every item:

- [ ] App launches and displays the main window.
- [ ] All 9+ banners appear in the horizontal scroll strip.
- [ ] Banner selection works (click, arrow buttons, mouse wheel scroll).
- [ ] Warp ×1 produces correct gacha results with proper rarity distribution.
- [ ] Warp ×10 always contains at least one 4★+ item.
- [ ] Gold pity counter increments correctly and resets on 5★ pull.
- [ ] Purple pity counter increments correctly and resets on 4★ pull.
- [ ] Soft pity ramp works (increased rates after pull 73 for avatars, 65 for LCs).
- [ ] 50/50 guarantee works for event avatars; 75/25 for event LCs.
- [ ] Off-rate ("missed") gold count and stats row show correctly.
- [ ] Result card displays correct item info, icons, and rarity-colored border.
- [ ] Previous/Next buttons navigate through pull history correctly.
- [ ] History list displays all pulled items with correct localization.
- [ ] Statistics show correct counts and percentages.
- [ ] Reset banner clears history, resets pity, shows confirmation dialog.
- [ ] Language switch (EN ↔ ZH) works at runtime; all text updates.
- [ ] Language preference persists across app restart.
- [ ] Window title, button labels, group headers all localize correctly.
- [ ] All element colors and rarity colors match the original.
- [ ] Dark theme visuals are identical (no color/shading/sizing changes).
- [ ] No crashes, unhandled exceptions, or binding errors in debug output.
- [ ] Error logging to `%LOCALAPPDATA%/HSR-Gacha-Simulator/error.log` still works.
