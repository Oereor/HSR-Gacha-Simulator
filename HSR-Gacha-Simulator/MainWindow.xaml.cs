using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace HSR_Gacha_Simulator
{
    public partial class MainWindow : Window
    {
        // ── ViewModel ───────────────────────────────────────────────
        private readonly MainViewModel viewModel = new();

        // ── Gacha system instances ──────────────────────────────────
        private GachaSystem currentSystem = null!;

        // ── Banner mapping ──────────────────────────────────────────
        private record BannerEntry(GachaSystem System, string LocKey);

        private Dictionary<RadioButton, BannerEntry> _bannerMap = null!;
        private string _currentBannerKey = "ui.banner.ordinary";

        // Prevents event handlers from running before initialisation is complete.
        private bool initialising;

        // ── Localisation shorthand ──────────────────────────────────
        private static LocalizationService L10n => LocalizationService.Instance;

        public MainWindow()
        {
            initialising = true;
            InitializeComponent();

            // Set up data binding
            DataContext = viewModel;
            lvHistory.ItemsSource = viewModel.HistoryItems;

            try
            {
                InitializeSystems();
                BuildBannerMap();
                currentSystem = _bannerMap[rbOrdinary].System;
                initialising = false;
                viewModel.SwitchBanner(currentSystem);
            }
            catch (Exception ex)
            {
                initialising = false;
                MessageBox.Show(
                    L10n.Get("dialog.error.init_failed", ex.Message),
                    L10n.Get("dialog.error.title"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                viewModel.StatusText = L10n.Get("ui.status.init_failed");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Initialisation
        // ═══════════════════════════════════════════════════════════════

        private void BuildBannerMap()
        {
            _bannerMap = new Dictionary<RadioButton, BannerEntry>
            {
                { rbOrdinary,      new(ordinarySystem,        "ui.banner.ordinary") },
                { rbCyreneAvatar,  new(cyreneSystem,          "ui.banner.cyrene_avatar") },
                { rbPhainonAvatar, new(phainonSystem,         "ui.banner.phainon_avatar") },
                { rbCyreneLC,      new(cyreneLightConeSystem, "ui.banner.cyrene_lc") },
                { rbPhainonLC,     new(phainonLightConeSystem,"ui.banner.phainon_lc") },
                { rbArcherAvatar,  new(archerAvatarSystem,    "ui.banner.archer_avatar") },
                { rbArcherLC,      new(archerLightConeSystem, "ui.banner.archer_lc") },
                { rbSaberAvatar,   new(saberAvatarSystem,     "ui.banner.saber_avatar") },
                { rbSaberLC,       new(saberLightConeSystem,  "ui.banner.saber_lc") },
                { rbAllGold,      new(allGoldSystem,        "ui.banner.all_gold") },
            };
        }

        private GachaSystem ordinarySystem = null!;
        private GachaSystem cyreneSystem = null!;
        private GachaSystem phainonSystem = null!;
        private GachaSystem cyreneLightConeSystem = null!;
        private GachaSystem phainonLightConeSystem = null!;
        private GachaSystem archerAvatarSystem = null!;
        private GachaSystem archerLightConeSystem = null!;
        private GachaSystem saberAvatarSystem = null!;
        private GachaSystem saberLightConeSystem = null!;
        private GachaSystem allGoldSystem = null!;

        private void InitializeSystems()
        {
            string poolDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PoolConfigs");

            // Load shared pools once
            var goldAvatars    = DataLoader.LoadFromFile(Path.Combine(poolDir, "OrdinaryGoldPoolConfig.json"))
                                            .Where(i => i.Type == ItemType.Avatar).ToList();
            var goldLightCones = DataLoader.LoadFromFile(Path.Combine(poolDir, "OrdinaryGoldPoolConfig.json"))
                                            .Where(i => i.Type == ItemType.LightCone).ToList();
            var celestialGold  = DataLoader.LoadFromFile(Path.Combine(poolDir, "CelestialGoldPoolConfig.json"));
            var purpleAvatars  = DataLoader.LoadFromFile(Path.Combine(poolDir, "OrdinaryPurplePoolConfig.json"))
                                            .Where(i => i.Type == ItemType.Avatar).ToList();
            var purpleLightCones = DataLoader.LoadFromFile(Path.Combine(poolDir, "OrdinaryPurplePoolConfig.json"))
                                              .Where(i => i.Type == ItemType.LightCone).ToList();
            var blueItems      = DataLoader.LoadFromFile(Path.Combine(poolDir, "BluePoolConfig.json"))
                                            .Where(i => i.Rarity == ItemRarity.Blue).ToList();

            // Ordinary banner
            ordinarySystem = GachaSystem.Create(GachaType.Ordinary);
            ordinarySystem.LoadPools(
                goldAvatars, goldLightCones,
                new List<ItemData>(), new List<ItemData>(),
                purpleAvatars, purpleLightCones,
                new List<ItemData>(),
                blueItems);

            // Event banners — one line each
            cyreneSystem           = CreateBannerSystem(GachaType.EventAvatar,    "CyreneEventAvatarPoolConfig.json",    goldAvatars, goldLightCones, celestialGold, purpleAvatars, purpleLightCones, blueItems);
            phainonSystem          = CreateBannerSystem(GachaType.EventAvatar,    "PhainonEventAvatarPoolConfig.json",   goldAvatars, goldLightCones, celestialGold, purpleAvatars, purpleLightCones, blueItems);
            cyreneLightConeSystem  = CreateBannerSystem(GachaType.EventLightCone, "CyreneEventLightConePoolConfig.json",  goldAvatars, goldLightCones, celestialGold, purpleAvatars, purpleLightCones, blueItems);
            phainonLightConeSystem = CreateBannerSystem(GachaType.EventLightCone, "PhainonEventLightConePoolConfig.json", goldAvatars, goldLightCones, celestialGold, purpleAvatars, purpleLightCones, blueItems);
            archerAvatarSystem     = CreateBannerSystem(GachaType.EventAvatar,    "ArcherEventAvatarPoolConfig.json",    goldAvatars, goldLightCones, celestialGold, purpleAvatars, purpleLightCones, blueItems, hasEventPurple: false);
            archerLightConeSystem  = CreateBannerSystem(GachaType.EventLightCone, "ArcherEventLightConePoolConfig.json",  goldAvatars, goldLightCones, celestialGold, purpleAvatars, purpleLightCones, blueItems, hasEventPurple: false);
            saberAvatarSystem      = CreateBannerSystem(GachaType.EventAvatar,    "SaberEventAvatarPoolConfig.json",     goldAvatars, goldLightCones, celestialGold, purpleAvatars, purpleLightCones, blueItems, hasEventPurple: false);
            saberLightConeSystem   = CreateBannerSystem(GachaType.EventLightCone, "SaberEventLightConePoolConfig.json",   goldAvatars, goldLightCones, celestialGold, purpleAvatars, purpleLightCones, blueItems, hasEventPurple: false);

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
        }

        private GachaSystem CreateBannerSystem(
            GachaType type,
            string poolConfigFileName,
            List<ItemData> goldAvatars,
            List<ItemData> goldLightCones,
            List<ItemData> celestialGold,
            List<ItemData> purpleAvatars,
            List<ItemData> purpleLightCones,
            List<ItemData> blueItems,
            bool hasEventPurple = true)
        {
            string poolDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PoolConfigs");
            var raw = DataLoader.LoadFromFile(Path.Combine(poolDir, poolConfigFileName));

            var eventGold = raw.Where(i => i.Rarity == ItemRarity.Gold).ToList();
            var eventPurple = hasEventPurple
                ? raw.Where(i => i.Rarity == ItemRarity.Purple).ToList()
                : new List<ItemData>();

            var system = GachaSystem.Create(type);

            if (type == GachaType.EventLightCone)
            {
                system.LoadPools(
                    goldAvatars, goldLightCones,
                    new List<ItemData>(),   // no celestial pool for light cone banners
                    eventGold,
                    purpleAvatars, purpleLightCones,
                    eventPurple,
                    blueItems);
            }
            else
            {
                system.LoadPools(
                    goldAvatars, goldLightCones,
                    celestialGold,
                    eventGold,
                    purpleAvatars, purpleLightCones,
                    eventPurple,
                    blueItems);
            }

            return system;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Event handlers
        // ═══════════════════════════════════════════════════════════════

        private async void Banner_Changed(object sender, RoutedEventArgs e)
        {
            if (initialising) return;

            if (sender is RadioButton rb && _bannerMap.TryGetValue(rb, out var entry))
            {
                currentSystem = entry.System;
                _currentBannerKey = entry.LocKey;
                await viewModel.SwitchBannerAsync(currentSystem);
            }
        }

        private void BtnWarp1_Click(object sender, RoutedEventArgs e)
        {
            if (initialising || viewModel.IsLoading) return;

            viewModel.StatusText = L10n.Get("ui.status.pulling");
            currentSystem.Pull();
            viewModel.CurrentResultIndex = currentSystem.History.Count - 1;
            viewModel.AfterPull(1);
        }

        private void BtnWarp10_Click(object sender, RoutedEventArgs e)
        {
            if (initialising || viewModel.IsLoading) return;

            viewModel.StatusText = L10n.Get("ui.status.pulling_x10");
            currentSystem.Pull10();
            viewModel.CurrentResultIndex = currentSystem.History.Count - 1;
            viewModel.AfterPull(10);
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            string bannerName = L10n.Get(_currentBannerKey);

            var result = MessageBox.Show(
                L10n.Get("dialog.reset_banner.message", bannerName),
                L10n.Get("dialog.reset_banner.title"),
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.OK)
                return;

            currentSystem.Reset();
            viewModel.ResetBanner(bannerName);
        }

        private void BtnPrevResult_Click(object sender, RoutedEventArgs e)
        {
            viewModel.NavigatePrev();
        }

        private void BtnNextResult_Click(object sender, RoutedEventArgs e)
        {
            viewModel.NavigateNext();
        }
    }
}
