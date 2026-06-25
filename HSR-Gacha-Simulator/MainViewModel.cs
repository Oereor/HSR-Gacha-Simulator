using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace HSR_Gacha_Simulator
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // ── Cached brushes ──────────────────────────────────────────
        private static readonly SolidColorBrush GoldBrush = new(Color.FromRgb(0xFF, 0xD7, 0x00));
        private static readonly SolidColorBrush DimBrush = new(Color.FromRgb(0xC0, 0xC0, 0xC0));
        private static readonly SolidColorBrush DefaultCardBorderBrush = new(Color.FromRgb(0x3A, 0x3A, 0x6E));

        // ── Localisation shorthand ──────────────────────────────────
        private static LocalizationService L10n => LocalizationService.Instance;

        // ── Banners ─────────────────────────────────────────────────
        /// <summary>All banners shown in the horizontal scroll strip, in display order.</summary>
        public ObservableCollection<BannerInfo> Banners { get; } = new();

        private BannerInfo? _selectedBanner;
        private bool _initialising;

        /// <summary>The currently selected banner. Set by the ListBox binding.</summary>
        public BannerInfo? SelectedBanner
        {
            get => _selectedBanner;
            set
            {
                if (_selectedBanner == value) return;
                if (_selectedBanner != null)
                    _selectedBanner.IsSelected = false;
                _selectedBanner = value;
                if (_selectedBanner != null)
                    _selectedBanner.IsSelected = true;
                OnPropertyChanged();
                if (!_initialising)
                    OnBannerSwitched(value);
            }
        }

        private GachaSystem CurrentSystem => _selectedBanner?.System ?? Banners[0].System;

        // ── History ─────────────────────────────────────────────────
        public ObservableCollection<HistoryItemDisplay> HistoryItems { get; } = new();

        private int _currentResultIndex = -1;
        public int CurrentResultIndex
        {
            get => _currentResultIndex;
            set { _currentResultIndex = value; OnPropertyChanged(); }
        }

        // ── Loading state ──────────────────────────────────────────
        private bool _isLoading;

        /// <summary>
        /// True while a history reload is in progress.
        /// Bind UI controls to this to disable them during loading.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        // ── Status ──────────────────────────────────────────────────
        private string _statusText = "";
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        // ── Pity: Gold ──────────────────────────────────────────────
        private string _goldPity = "0";
        public string GoldPity
        {
            get => _goldPity;
            set { _goldPity = value; OnPropertyChanged(); }
        }

        private string _goldGuarantee = "";
        public string GoldGuarantee
        {
            get => _goldGuarantee;
            set { _goldGuarantee = value; OnPropertyChanged(); }
        }

        private Brush _goldGuaranteeBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0));
        public Brush GoldGuaranteeBrush
        {
            get => _goldGuaranteeBrush;
            set { _goldGuaranteeBrush = value; OnPropertyChanged(); }
        }

        // ── Pity: Purple ────────────────────────────────────────────
        private string _purplePity = "0";
        public string PurplePity
        {
            get => _purplePity;
            set { _purplePity = value; OnPropertyChanged(); }
        }

        private string _purpleGuarantee = "";
        public string PurpleGuarantee
        {
            get => _purpleGuarantee;
            set { _purpleGuarantee = value; OnPropertyChanged(); }
        }

        private Brush _purpleGuaranteeBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0));
        public Brush PurpleGuaranteeBrush
        {
            get => _purpleGuaranteeBrush;
            set { _purpleGuaranteeBrush = value; OnPropertyChanged(); }
        }

        // ── Statistics ──────────────────────────────────────────────
        private string _totalPulls = "";
        public string TotalPulls
        {
            get => _totalPulls;
            set { _totalPulls = value; OnPropertyChanged(); }
        }

        private string _goldCount = "0";
        public string GoldCount
        {
            get => _goldCount;
            set { _goldCount = value; OnPropertyChanged(); }
        }

        private string _goldRate = "";
        public string GoldRate
        {
            get => _goldRate;
            set { _goldRate = value; OnPropertyChanged(); }
        }

        private string _purpleCount = "0";
        public string PurpleCount
        {
            get => _purpleCount;
            set { _purpleCount = value; OnPropertyChanged(); }
        }

        private string _purpleRate = "";
        public string PurpleRate
        {
            get => _purpleRate;
            set { _purpleRate = value; OnPropertyChanged(); }
        }

        private string _blueCount = "0";
        public string BlueCount
        {
            get => _blueCount;
            set { _blueCount = value; OnPropertyChanged(); }
        }

        private string _blueRate = "";
        public string BlueRate
        {
            get => _blueRate;
            set { _blueRate = value; OnPropertyChanged(); }
        }

        private string _missedGoldStats = "";
        private Visibility _missedStatsVisibility = Visibility.Collapsed;

        /// <summary>
        /// Formatted missed-gold string, e.g. "歪: 2/5 (40.0%)" (ZH) / "Missed: 2/5 (40.0%)" (EN).
        /// </summary>
        public string MissedGoldStats
        {
            get => _missedGoldStats;
            set { _missedGoldStats = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Visibility for the missed-stats row. Visible for event banners, collapsed for ordinary.
        /// </summary>
        public Visibility MissedStatsVisibility
        {
            get => _missedStatsVisibility;
            set { _missedStatsVisibility = value; OnPropertyChanged(); }
        }

        // ── Result Card ─────────────────────────────────────────────
        private string _resultRarity = "";
        public string ResultRarity
        {
            get => _resultRarity;
            set { _resultRarity = value; OnPropertyChanged(); }
        }

        private Brush _resultRarityBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
        public Brush ResultRarityBrush
        {
            get => _resultRarityBrush;
            set { _resultRarityBrush = value; OnPropertyChanged(); }
        }

        private string _resultName = "";
        public string ResultName
        {
            get => _resultName;
            set { _resultName = value; OnPropertyChanged(); }
        }

        private string _resultType = "";
        public string ResultType
        {
            get => _resultType;
            set { _resultType = value; OnPropertyChanged(); }
        }

        private string _resultPath = "";
        public string ResultPath
        {
            get => _resultPath;
            set { _resultPath = value; OnPropertyChanged(); }
        }

        private string _resultElement = "";
        public string ResultElement
        {
            get => _resultElement;
            set { _resultElement = value; OnPropertyChanged(); }
        }

        private Brush _resultElementBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0));
        public Brush ResultElementBrush
        {
            get => _resultElementBrush;
            set { _resultElementBrush = value; OnPropertyChanged(); }
        }

        private ImageSource? _resultPathIcon;
        public ImageSource? ResultPathIcon
        {
            get => _resultPathIcon;
            set { _resultPathIcon = value; OnPropertyChanged(); }
        }

        private Visibility _resultPathIconVisibility = Visibility.Collapsed;
        public Visibility ResultPathIconVisibility
        {
            get => _resultPathIconVisibility;
            set { _resultPathIconVisibility = value; OnPropertyChanged(); }
        }

        private ImageSource? _resultElementIcon;
        public ImageSource? ResultElementIcon
        {
            get => _resultElementIcon;
            set { _resultElementIcon = value; OnPropertyChanged(); }
        }

        private Visibility _resultElementIconVisibility = Visibility.Collapsed;
        public Visibility ResultElementIconVisibility
        {
            get => _resultElementIconVisibility;
            set { _resultElementIconVisibility = value; OnPropertyChanged(); }
        }

        private Brush _resultCardBorderBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x6E));
        public Brush ResultCardBorderBrush
        {
            get => _resultCardBorderBrush;
            set { _resultCardBorderBrush = value; OnPropertyChanged(); }
        }

        private string _resultIndexText = "";
        public string ResultIndexText
        {
            get => _resultIndexText;
            set { _resultIndexText = value; OnPropertyChanged(); }
        }

        private Visibility _dotElementVisibility = Visibility.Visible;
        public Visibility DotElementVisibility
        {
            get => _dotElementVisibility;
            set { _dotElementVisibility = value; OnPropertyChanged(); }
        }

        private Visibility _resultElementTextVisibility = Visibility.Visible;
        public Visibility ResultElementTextVisibility
        {
            get => _resultElementTextVisibility;
            set { _resultElementTextVisibility = value; OnPropertyChanged(); }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Initialisation
        // ═══════════════════════════════════════════════════════════════

        public void InitializeSystems()
        {
            _initialising = true;

            string poolDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PoolConfigs");

            // ── Load shared pools ─────────────────────────────────────
            var ordinaryGoldRaw   = DataLoader.LoadFromFile(Path.Combine(poolDir, "OrdinaryGoldPoolConfig.json"));
            var ordinaryPurpleRaw = DataLoader.LoadFromFile(Path.Combine(poolDir, "OrdinaryPurplePoolConfig.json"));
            var celestialGoldRaw  = DataLoader.LoadFromFile(Path.Combine(poolDir, "CelestialGoldPoolConfig.json"));
            var blueRaw           = DataLoader.LoadFromFile(Path.Combine(poolDir, "BluePoolConfig.json"));
            var allGoldRaw        = DataLoader.LoadFromFile(Path.Combine(poolDir, "AllGoldPoolConfig.json"));

            var goldAvatars       = ordinaryGoldRaw.Where(i => i.Type == ItemType.Avatar).ToList();
            var goldLightCones    = ordinaryGoldRaw.Where(i => i.Type == ItemType.LightCone).ToList();
            var purpleAvatars     = ordinaryPurpleRaw.Where(i => i.Type == ItemType.Avatar).ToList();
            var purpleLightCones  = ordinaryPurpleRaw.Where(i => i.Type == ItemType.LightCone).ToList();
            var blueItems         = blueRaw.Where(i => i.Rarity == ItemRarity.Blue).ToList();
            var allGoldAvatars    = allGoldRaw.Where(i => i.Type == ItemType.Avatar).ToList();
            var allGoldLightCones = allGoldRaw.Where(i => i.Type == ItemType.LightCone).ToList();

            // ── Helper: create a system ───────────────────────────────
            GachaSystem CreateSystem(GachaType type,
                List<ItemData> eventGold, List<ItemData> eventPurple,
                List<ItemData>? celestial = null)
            {
                var sys = GachaSystem.Create(type);
                sys.LoadPools(goldAvatars, goldLightCones,
                    celestial ?? new List<ItemData>(),
                    eventGold, purpleAvatars, purpleLightCones,
                    eventPurple, blueItems);
                return sys;
            }

            // ── Ordinary ──────────────────────────────────────────────
            Banners.Add(new BannerInfo
            {
                System = CreateSystem(GachaType.Ordinary,
                    new List<ItemData>(), new List<ItemData>()),
                BannerKey = "ordinary", BannerTitle = "Ordinary",
                GachaType = GachaType.Ordinary
            });

            // ── All Gold ──────────────────────────────────────────────
            Banners.Add(new BannerInfo
            {
                System = CreateSystem(GachaType.Ordinary,
                    new List<ItemData>(), new List<ItemData>()),
                BannerKey = "all_gold", BannerTitle = "All Gold (Expanded Pool)",
                GachaType = GachaType.Ordinary
            });
            // Patch the All-Gold system's gold pools after creation
            var agSys = Banners[^1].System;
            agSys.LoadPools(allGoldAvatars, allGoldLightCones,
                new List<ItemData>(), new List<ItemData>(),
                purpleAvatars, purpleLightCones,
                new List<ItemData>(), blueItems);

            // ── Event banners (data-driven) ───────────────────────────
            // Build master lookup from the Gold + Purple full-data files
            var masterLookup = DataLoader.BuildMasterLookup(
                Path.Combine(poolDir, "AllGoldPoolConfig.json"),
                Path.Combine(poolDir, "OrdinaryPurplePoolConfig.json"));

            var eventConfigs = DataLoader.LoadEventPoolConfigs(
                Path.Combine(poolDir, "EventPoolConfigs.json"),
                masterLookup);

            foreach (var config in eventConfigs)
            {
                var goldItems = config.Items.Where(i => i.Rarity == ItemRarity.Gold).ToList();
                if (goldItems.Count == 0) continue;

                GachaType gachaType = goldItems.Any(i => i.Type == ItemType.Avatar)
                    ? GachaType.EventAvatar
                    : GachaType.EventLightCone;

                var eventGold   = config.Items.Where(i => i.Rarity == ItemRarity.Gold).ToList();
                var eventPurple = config.Items.Where(i => i.Rarity == ItemRarity.Purple).ToList();

                Banners.Add(new BannerInfo
                {
                    System = CreateSystem(gachaType, eventGold, eventPurple,
                        celestial: gachaType == GachaType.EventAvatar ? celestialGoldRaw : null),
                    BannerKey   = config.BannerKey,
                    BannerTitle = config.BannerTitle,
                    GachaType   = gachaType
                });
            }

            _initialising = false;

            // Select the first banner by default (via the setter so IsSelected propagates)
            if (Banners.Count > 0)
                SelectedBanner = Banners[0];
        }

        // ═══════════════════════════════════════════════════════════════
        //  Public methods — called from code-behind
        // ═══════════════════════════════════════════════════════════════

        public string CurrentBannerKey => _selectedBanner?.BannerKey ?? "ordinary";

        /// <summary>Perform a pull (×1 or ×10).</summary>
        public void Pull(int count)
        {
            if (count == 1)
            {
                CurrentSystem.Pull();
            }
            else
            {
                CurrentSystem.Pull10();
            }
            CurrentResultIndex = CurrentSystem.History.Count - 1;
            AfterPull(count);
        }

        /// <summary>Reset the current banner (called from code-behind after confirmation).</summary>
        public void ResetCurrentBanner()
        {
            CurrentSystem.Reset();
            ResetBanner(L10n.Get($"ui.banner.{CurrentBannerKey}"));
        }

        /// <summary>Called after every pull (×1 or ×10).</summary>
        public void AfterPull(int count)
        {
            AppendNewHistoryItems(count);
            RefreshPity();
            RefreshStatistics();
            ShowLatestOrStoredResult();
            StatusText = L10n.Get("ui.status.ready");
        }

        /// <summary>Navigate to previous result in history.</summary>
        public void NavigatePrev()
        {
            if (CurrentSystem.History.Count == 0)
                return;

            int idx = CurrentResultIndex - 1;
            if (idx < 0)
                idx = CurrentSystem.History.Count - 1;

            ShowResult(idx);
        }

        /// <summary>Navigate to next result in history.</summary>
        public void NavigateNext()
        {
            if (CurrentSystem.History.Count == 0)
                return;

            int idx = CurrentResultIndex + 1;
            if (idx >= CurrentSystem.History.Count)
                idx = 0;

            ShowResult(idx);
        }

        /// <summary>Called after a banner reset.</summary>
        public void ResetBanner(string bannerName)
        {
            CurrentResultIndex = -1;
            ClearResult();
            ClearHistory();
            RefreshPity();
            RefreshStatistics();
            StatusText = L10n.Get("ui.status.banner_reset", bannerName);
        }

        private void OnBannerSwitched(BannerInfo? banner)
        {
            if (banner == null) return;
            CurrentResultIndex = -1;
            ClearResult();
            _ = ReloadAllHistoryAsync();
            RefreshPity();
            RefreshStatistics();
            StatusText = L10n.Get("ui.status.ready");
        }

        // ═══════════════════════════════════════════════════════════════
        //  History helpers
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Append the most recent <paramref name="count"/> pulls to the top of the list.</summary>
        public void AppendNewHistoryItems(int count)
        {
            var history = CurrentSystem.History;
            // 'count' new items were added to the end of History
            for (int i = history.Count - count; i < history.Count; i++)
            {
                HistoryItems.Insert(0, HistoryItemDisplay.FromItemData(history[i], i + 1));
            }
        }

        /// <summary>Clear the history list (used after reset when list is already empty).</summary>
        public void ClearHistory()
        {
            HistoryItems.Clear();
        }

        /// <summary>
        /// Asynchronously reloads the entire history list from the current
        /// <see cref="GachaSystem.History"/>.  The heavy work (creating
        /// <see cref="HistoryItemDisplay"/> objects) runs on a thread-pool
        /// thread so the UI stays responsive.  The final
        /// <see cref="ObservableCollection{T}"/> update is dispatched back
        /// to the UI thread.
        /// </summary>
        public async Task ReloadAllHistoryAsync()
        {
            var sys = CurrentSystem;
            if (sys == null) return;

            IsLoading = true;
            StatusText = L10n.Get("ui.status.loading");

            // Capture the history snapshot on the UI thread before
            // dispatching to the thread pool.  This avoids cross-thread
            // access to GachaSystem.History (which is not thread-safe).
            var snapshot = sys.History.ToArray();

            List<HistoryItemDisplay> newItems;

            try
            {
                // CPU-bound work — runs off the UI thread
                newItems = await Task.Run(() =>
                {
                    var list = new List<HistoryItemDisplay>(snapshot.Length);
                    // Newest first (same ordering as the original RefreshHistory)
                    for (int i = snapshot.Length - 1; i >= 0; i--)
                    {
                        list.Add(HistoryItemDisplay.FromItemData(snapshot[i], i + 1));
                    }
                    return list;
                });

                // UI-thread work
                HistoryItems.Clear();
                foreach (var item in newItems)
                {
                    HistoryItems.Add(item);
                }
            }
            finally
            {
                IsLoading = false;
                StatusText = L10n.Get("ui.status.ready");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Pity display
        // ═══════════════════════════════════════════════════════════════

        public void RefreshPity()
        {
            var sys = CurrentSystem;

            GoldPity = sys.NonGoldGachaCount.ToString();
            GoldGuarantee = sys.IsGuaranteed
                ? L10n.Get("ui.pity.guaranteed")
                : L10n.Get("ui.pity.not_guaranteed");
            GoldGuaranteeBrush = sys.IsGuaranteed ? GoldBrush : DimBrush;

            PurplePity = sys.NonPurpleGachaCount.ToString();
            PurpleGuarantee = sys.IsPurpleGuaranteed
                ? L10n.Get("ui.pity.guaranteed")
                : L10n.Get("ui.pity.not_guaranteed");
            PurpleGuaranteeBrush = sys.IsPurpleGuaranteed ? GoldBrush : DimBrush;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Statistics display
        // ═══════════════════════════════════════════════════════════════

        public void RefreshStatistics()
        {
            var sys = CurrentSystem;
            int total = sys.TotalPulls;
            TotalPulls = L10n.Get("ui.stats.total_pulls", total);

            if (total == 0)
            {
                GoldCount = "0"; GoldRate = "  (—)";
                PurpleCount = "0"; PurpleRate = "  (—)";
                BlueCount = "0"; BlueRate = "  (—)";
                MissedGoldStats = "";
                MissedStatsVisibility = Visibility.Collapsed;
            }
            else
            {
                int gold = sys.GoldCount, purple = sys.PurpleCount, blue = sys.BlueCount;
                GoldCount = gold.ToString();
                GoldRate = $"  ({gold * 100.0 / total:F1}%)";
                PurpleCount = purple.ToString();
                PurpleRate = $"  ({purple * 100.0 / total:F1}%)";
                BlueCount = blue.ToString();
                BlueRate = $"  ({blue * 100.0 / total:F1}%)";

                // ── Missed gold stats ("歪") ───────────────────────
                if (sys.HasEventItems && gold > 0)
                {
                    int missed = sys.MissedGoldCount;
                    double rate = missed * 100.0 / gold;
                    MissedGoldStats = L10n.Get("ui.stats.missed", missed, gold, rate);
                    MissedStatsVisibility = Visibility.Visible;
                }
                else
                {
                    MissedGoldStats = "";
                    MissedStatsVisibility = Visibility.Collapsed;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Result card
        // ═══════════════════════════════════════════════════════════════

        public void ShowResult(int index)
        {
            var sys = CurrentSystem;
            if (sys.History.Count == 0 || index < 0 || index >= sys.History.Count)
            {
                ClearResult();
                return;
            }

            CurrentResultIndex = index;
            var item = sys.History[index];

            // Rarity
            ResultRarity = HistoryItemDisplay.FromItemData(item, 0).RarityStars;
            ResultRarityBrush = RarityConverters.GetForegroundBrush(item.Rarity);

            // Name
            ResultName = L10n.GetItemName(item.Name);

            // Type
            ResultType = item.Type == ItemType.Avatar
                ? L10n.Get("ui.result.type.avatar")
                : L10n.Get("ui.result.type.lightcone");

            // Path
            ResultPath = HistoryItemDisplay.FormatPath(item.Path);

            // Element
            if (item.Type == ItemType.LightCone || item.ElementType == ElementType.Unknown)
            {
                ResultElement = "—";
                ResultElementBrush = DimBrush;
            }
            else
            {
                ResultElement = HistoryItemDisplay.FormatElement(item.ElementType, item.Type);
                ResultElementBrush = ElementTypeToBrushConverter.GetBrush(item.ElementType);
            }

            // Icons
            ResultPathIcon = IconLoader.LoadOrNull($"Path_{item.Path}.png");
            ResultPathIconVisibility = ResultPathIcon != null ? Visibility.Visible : Visibility.Collapsed;

            if (item.Type == ItemType.LightCone || item.ElementType == ElementType.Unknown)
            {
                ResultElementIcon = null;
                ResultElementIconVisibility = Visibility.Collapsed;
            }
            else
            {
                ResultElementIcon = IconLoader.LoadOrNull($"Element_{item.ElementType}.png");
                ResultElementIconVisibility = ResultElementIcon != null ? Visibility.Visible : Visibility.Collapsed;
            }

            // Pull number (displayed between nav buttons)
            ResultIndexText = L10n.Get("ui.result.pull_number", index + 1, sys.History.Count);

            // Border
            ResultCardBorderBrush = RarityConverters.GetBorderBrush(item.Rarity);

            DotElementVisibility = Visibility.Visible;
            ResultElementTextVisibility = Visibility.Visible;
        }

        public void ClearResult()
        {
            ResultRarity = "";
            ResultName = L10n.Get("ui.result.default");
            ResultType = "";
            ResultPath = "";
            ResultElement = "";
            ResultIndexText = "";
            ResultPathIcon = null;
            ResultPathIconVisibility = Visibility.Collapsed;
            ResultElementIcon = null;
            ResultElementIconVisibility = Visibility.Collapsed;
            ResultCardBorderBrush = DefaultCardBorderBrush;
            DotElementVisibility = Visibility.Visible;
            ResultElementTextVisibility = Visibility.Visible;
        }

        private void ShowLatestOrStoredResult()
        {
            var sys = CurrentSystem;
            if (sys.History.Count == 0)
            {
                ClearResult();
                return;
            }

            if (CurrentResultIndex < 0 || CurrentResultIndex >= sys.History.Count)
                CurrentResultIndex = sys.History.Count - 1;

            ShowResult(CurrentResultIndex);
        }

        // ═══════════════════════════════════════════════════════════════
        //  INotifyPropertyChanged
        // ═══════════════════════════════════════════════════════════════

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  BannerInfo — ViewModel for one selectable banner pill
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// ViewModel for one selectable banner in the horizontal scroll strip.
    /// </summary>
    public class BannerInfo : INotifyPropertyChanged
    {
        private bool _isSelected;

        /// <summary>The GachaSystem backing this banner.</summary>
        public GachaSystem System { get; set; } = null!;

        /// <summary>Stable key for localization, e.g. "cyrene_avatar".</summary>
        public string BannerKey { get; set; } = "";

        /// <summary>English display title, e.g. "Cyrene (Avatar)" — fallback if loc key missing.</summary>
        public string BannerTitle { get; set; } = "";

        /// <summary>True when this banner is the currently active one.</summary>
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        /// <summary>Avatar or LightCone — determines probability model.</summary>
        public GachaType GachaType { get; set; }

        /// <summary>Localized display name for live language switching.</summary>
        public string DisplayName =>
            LocalizationService.Instance[$"ui.banner.{BannerKey}"] is string loc && loc != $"ui.banner.{BannerKey}"
                ? loc
                : BannerTitle;

        // ── INotifyPropertyChanged ──────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
