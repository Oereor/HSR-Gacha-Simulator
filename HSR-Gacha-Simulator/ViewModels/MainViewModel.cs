using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using HSR_Gacha_Simulator.Models;
using HSR_Gacha_Simulator.Services;

namespace HSR_Gacha_Simulator.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ILocalizationService _localizationService;
        private readonly IPoolDataService _poolDataService;
        private readonly IIconService _iconService;

        // ── Child ViewModels ────────────────────────────────────────
        public PityStatisticsViewModel PityStats { get; }
        public ResultCardViewModel ResultCard { get; }
        public HistoryPanelViewModel HistoryPanel { get; }

        // ── Banners ─────────────────────────────────────────────────
        public ObservableCollection<BannerViewModel> Banners { get; } = new();

        private BannerViewModel? _selectedBanner;
        private bool _initialising;

        public BannerViewModel? SelectedBanner
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

        public GachaSystem CurrentSystem =>
            _selectedBanner?.System ?? Banners[0].System;

        public string CurrentBannerKey =>
            _selectedBanner?.BannerKey ?? "ordinary";

        // ── Loading state ───────────────────────────────────────────
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // ── Status ──────────────────────────────────────────────────
        private string _statusText = "";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Constructor
        // ═══════════════════════════════════════════════════════════════

        public MainViewModel(
            ILocalizationService localizationService,
            IPoolDataService poolDataService,
            IIconService iconService)
        {
            _localizationService = localizationService;
            _poolDataService = poolDataService;
            _iconService = iconService;

            PityStats = new PityStatisticsViewModel(localizationService);
            ResultCard = new ResultCardViewModel(localizationService, iconService);
            HistoryPanel = new HistoryPanelViewModel(localizationService);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Initialisation
        // ═══════════════════════════════════════════════════════════════

        public void InitializeSystems()
        {
            _initialising = true;

            string poolDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PoolConfigs");

            // ── Load shared pools ─────────────────────────────────────
            var ordinaryGoldRaw   = _poolDataService.LoadFromFile(Path.Combine(poolDir, "OrdinaryGoldPoolConfig.json"));
            var ordinaryPurpleRaw = _poolDataService.LoadFromFile(Path.Combine(poolDir, "OrdinaryPurplePoolConfig.json"));
            var celestialGoldRaw  = _poolDataService.LoadFromFile(Path.Combine(poolDir, "CelestialGoldPoolConfig.json"));
            var blueRaw           = _poolDataService.LoadFromFile(Path.Combine(poolDir, "BluePoolConfig.json"));
            var allGoldRaw        = _poolDataService.LoadFromFile(Path.Combine(poolDir, "AllGoldPoolConfig.json"));

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
            Banners.Add(new BannerViewModel(
                CreateSystem(GachaType.Ordinary,
                    new List<ItemData>(), new List<ItemData>()),
                "ordinary", "Ordinary",
                GachaType.Ordinary));

            // ── All Gold ──────────────────────────────────────────────
            Banners.Add(new BannerViewModel(
                CreateSystem(GachaType.Ordinary,
                    new List<ItemData>(), new List<ItemData>()),
                "all_gold", "All Gold (Expanded Pool)",
                GachaType.Ordinary));
            // Patch the All-Gold system's gold pools after creation
            var agSys = Banners[^1].System;
            agSys.LoadPools(allGoldAvatars, allGoldLightCones,
                new List<ItemData>(), new List<ItemData>(),
                purpleAvatars, purpleLightCones,
                new List<ItemData>(), blueItems);

            // ── Event banners (data-driven) ───────────────────────────
            var masterLookup = _poolDataService.BuildMasterLookup(
                Path.Combine(poolDir, "AllGoldPoolConfig.json"),
                Path.Combine(poolDir, "OrdinaryPurplePoolConfig.json"));

            var eventConfigs = _poolDataService.LoadEventPoolConfigs(
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

                Banners.Add(new BannerViewModel(
                    CreateSystem(gachaType, eventGold, eventPurple,
                        celestial: gachaType == GachaType.EventAvatar ? celestialGoldRaw : null),
                    config.BannerKey,
                    config.BannerTitle,
                    gachaType));
            }

            _initialising = false;

            // Select the first banner by default
            if (Banners.Count > 0)
                SelectedBanner = Banners[0];
        }

        // ═══════════════════════════════════════════════════════════════
        //  Public methods
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Perform a pull (×1 or ×10).</summary>
        public void Pull(int count)
        {
            var sys = CurrentSystem;
            if (count == 1)
                sys.Pull();
            else
                sys.Pull10();

            HistoryPanel.CurrentResultIndex = sys.History.Count - 1;
            AfterPull(count);
        }

        /// <summary>Reset the current banner (called from code-behind after confirmation).</summary>
        public void ResetCurrentBanner()
        {
            CurrentSystem.Reset();
            ResetBanner(_localizationService.Get($"ui.banner.{CurrentBannerKey}"));
        }

        /// <summary>Called after every pull (×1 or ×10).</summary>
        public void AfterPull(int count)
        {
            var sys = CurrentSystem;
            HistoryPanel.AppendNewHistoryItems(sys, count);
            PityStats.Refresh(sys);
            ShowLatestOrStoredResult();
            StatusText = _localizationService.Get("ui.status.ready");
        }

        /// <summary>Navigate to previous result in history.</summary>
        public void NavigatePrev()
        {
            var sys = CurrentSystem;
            if (sys.History.Count == 0) return;
            int newIdx = ResultCard.NavigatePrev(HistoryPanel.CurrentResultIndex, sys.History.Count);
            HistoryPanel.CurrentResultIndex = newIdx;
            ResultCard.ShowResult(sys, newIdx);
        }

        /// <summary>Navigate to next result in history.</summary>
        public void NavigateNext()
        {
            var sys = CurrentSystem;
            if (sys.History.Count == 0) return;
            int newIdx = ResultCard.NavigateNext(HistoryPanel.CurrentResultIndex, sys.History.Count);
            HistoryPanel.CurrentResultIndex = newIdx;
            ResultCard.ShowResult(sys, newIdx);
        }

        /// <summary>Called after a banner reset.</summary>
        public void ResetBanner(string bannerName)
        {
            HistoryPanel.CurrentResultIndex = -1;
            ResultCard.ClearResult();
            HistoryPanel.ClearHistory();
            PityStats.Refresh(CurrentSystem);
            StatusText = _localizationService.Get("ui.status.banner_reset", bannerName);
        }

        private void OnBannerSwitched(BannerViewModel? banner)
        {
            if (banner == null) return;
            HistoryPanel.CurrentResultIndex = -1;
            ResultCard.ClearResult();
            _ = HistoryPanel.ReloadAllHistoryAsync(CurrentSystem);
            PityStats.Refresh(CurrentSystem);
            StatusText = _localizationService.Get("ui.status.ready");
        }

        private void ShowLatestOrStoredResult()
        {
            var sys = CurrentSystem;
            if (sys.History.Count == 0)
            {
                ResultCard.ClearResult();
                return;
            }

            if (HistoryPanel.CurrentResultIndex < 0 || HistoryPanel.CurrentResultIndex >= sys.History.Count)
                HistoryPanel.CurrentResultIndex = sys.History.Count - 1;

            ResultCard.ShowResult(sys, HistoryPanel.CurrentResultIndex);
        }
    }
}
