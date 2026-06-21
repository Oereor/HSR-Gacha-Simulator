using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HSR_Gacha_Simulator
{
    public partial class MainWindow : Window
    {
        // ── Gacha system instances ───────────────────────────────
        private GachaSystem ordinarySystem = null!;
        private GachaSystem cyreneSystem = null!;
        private GachaSystem phainonSystem = null!;
        private GachaSystem cyreneLightConeSystem = null!;
        private GachaSystem phainonLightConeSystem = null!;
        private GachaSystem archerAvatarSystem = null!;
        private GachaSystem archerLightConeSystem = null!;
        private GachaSystem saberAvatarSystem = null!;
        private GachaSystem saberLightConeSystem = null!;
        private GachaSystem currentSystem = null!;

        // ── Result-card navigation ───────────────────────────────
        private int currentResultIndex = -1; // -1 means "show the latest pull"

        // ── History data binding ─────────────────────────────────
        private readonly ObservableCollection<HistoryItemDisplay> historyItems = new();

        // Prevents event handlers from running before initialisation is complete.
        private bool initialising;

        public MainWindow()
        {
            initialising = true;
            InitializeComponent();
            lvHistory.ItemsSource = historyItems;

            try
            {
                InitializeSystems();
                currentSystem = ordinarySystem;
                initialising = false;
                UpdatePityDisplay();
                UpdateStatistics();
                txtStatus.Text = "Ready";
            }
            catch (Exception ex)
            {
                initialising = false;
                MessageBox.Show($"Failed to initialise: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                txtStatus.Text = "Initialisation failed";
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Initialisation
        // ═══════════════════════════════════════════════════════════

        private void InitializeSystems()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // 1. Load raw JSON lists
            string poolDir = Path.Combine(baseDir, "PoolConfigs");
            var ordinaryGoldRaw   = DataLoader.LoadFromFile(Path.Combine(poolDir, "OrdinaryGoldPoolConfig.json"));
            var ordinaryPurpleRaw = DataLoader.LoadFromFile(Path.Combine(poolDir, "OrdinaryPurplePoolConfig.json"));
            var cyreneRaw         = DataLoader.LoadFromFile(Path.Combine(poolDir, "CyreneEventAvatarPoolConfig.json"));
            var phainonRaw        = DataLoader.LoadFromFile(Path.Combine(poolDir, "PhainonEventAvatarPoolConfig.json"));
            var blueRaw           = DataLoader.LoadFromFile(Path.Combine(poolDir, "BluePoolConfig.json"));

            // 2. Split ordinary gold pool by type
            var goldAvatars    = ordinaryGoldRaw.Where(i => i.Type == ItemType.Avatar).ToList();
            var goldLightCones = ordinaryGoldRaw.Where(i => i.Type == ItemType.LightCone).ToList();

            // 3. Load celestial gold pool (used by EventAvatar banners for 50/50 losses)
            var celestialGoldRaw = DataLoader.LoadFromFile(Path.Combine(poolDir, "CelestialGoldPoolConfig.json"));

            // 4. Split ordinary purple pool by type
            var purpleAvatars    = ordinaryPurpleRaw.Where(i => i.Type == ItemType.Avatar).ToList();
            var purpleLightCones = ordinaryPurpleRaw.Where(i => i.Type == ItemType.LightCone).ToList();

            // 4b. Split event JSONs by rarity
            var cyreneGold   = cyreneRaw.Where(i => i.Rarity == ItemRarity.Gold).ToList();
            var cyrenePurple = cyreneRaw.Where(i => i.Rarity == ItemRarity.Purple).ToList();
            var phainonGold   = phainonRaw.Where(i => i.Rarity == ItemRarity.Gold).ToList();
            var phainonPurple = phainonRaw.Where(i => i.Rarity == ItemRarity.Purple).ToList();

            // Blue items need no splitting
            var blueItems = blueRaw.Where(i => i.Rarity == ItemRarity.Blue).ToList();

            // 5. Create and load the ordinary system
            ordinarySystem = GachaSystem.Create(GachaType.Ordinary);
            ordinarySystem.LoadPools(
                goldAvatars, goldLightCones,
                celestialGoldAvatars: new List<ItemData>(),
                eventGoldItems:   new List<ItemData>(),
                purpleAvatars, purpleLightCones,
                eventPurpleItems: new List<ItemData>(),
                blueItems);

            cyreneSystem = GachaSystem.Create(GachaType.EventAvatar);
            cyreneSystem.LoadPools(
                goldAvatars, goldLightCones,
                celestialGoldRaw,
                cyreneGold,
                purpleAvatars, purpleLightCones,
                cyrenePurple,
                blueItems);

            phainonSystem = GachaSystem.Create(GachaType.EventAvatar);
            phainonSystem.LoadPools(
                goldAvatars, goldLightCones,
                celestialGoldRaw,
                phainonGold,
                purpleAvatars, purpleLightCones,
                phainonPurple,
                blueItems);

            // 6. Load event light cone JSONs
            var cyreneLCRaw  = DataLoader.LoadFromFile(Path.Combine(poolDir, "CyreneEventLightConePoolConfig.json"));
            var phainonLCRaw = DataLoader.LoadFromFile(Path.Combine(poolDir, "PhainonEventLightConePoolConfig.json"));

            var cyreneLCGold   = cyreneLCRaw.Where(i => i.Rarity == ItemRarity.Gold).ToList();
            var cyreneLCPurple = cyreneLCRaw.Where(i => i.Rarity == ItemRarity.Purple).ToList();
            var phainonLCGold   = phainonLCRaw.Where(i => i.Rarity == ItemRarity.Gold).ToList();
            var phainonLCPurple = phainonLCRaw.Where(i => i.Rarity == ItemRarity.Purple).ToList();

            // 7. Create light cone banner systems
            cyreneLightConeSystem = GachaSystem.Create(GachaType.EventLightCone);
            cyreneLightConeSystem.LoadPools(
                goldAvatars, goldLightCones,
                new List<ItemData>(),
                cyreneLCGold,
                purpleAvatars, purpleLightCones,
                cyreneLCPurple,
                blueItems);

            phainonLightConeSystem = GachaSystem.Create(GachaType.EventLightCone);
            phainonLightConeSystem.LoadPools(
                goldAvatars, goldLightCones,
                new List<ItemData>(),
                phainonLCGold,
                purpleAvatars, purpleLightCones,
                phainonLCPurple,
                blueItems);

            // 8. Load Archer / Saber collaboration JSONs
            var archerAvatarRaw = DataLoader.LoadFromFile(Path.Combine(poolDir, "ArcherEventAvatarPoolConfig.json"));
            var archerLCRaw     = DataLoader.LoadFromFile(Path.Combine(poolDir, "ArcherEventLightConePoolConfig.json"));
            var saberAvatarRaw  = DataLoader.LoadFromFile(Path.Combine(poolDir, "SaberEventAvatarPoolConfig.json"));
            var saberLCRaw      = DataLoader.LoadFromFile(Path.Combine(poolDir, "SaberEventLightConePoolConfig.json"));

            var archerAvatarGold = archerAvatarRaw.Where(i => i.Rarity == ItemRarity.Gold).ToList();
            var archerLCGold     = archerLCRaw.Where(i => i.Rarity == ItemRarity.Gold).ToList();
            var saberAvatarGold  = saberAvatarRaw.Where(i => i.Rarity == ItemRarity.Gold).ToList();
            var saberLCGold      = saberLCRaw.Where(i => i.Rarity == ItemRarity.Gold).ToList();

            // 9. Create Archer / Saber banner systems (no event purple items)
            archerAvatarSystem = GachaSystem.Create(GachaType.EventAvatar);
            archerAvatarSystem.LoadPools(
                goldAvatars, goldLightCones,
                celestialGoldRaw,
                archerAvatarGold,
                purpleAvatars, purpleLightCones,
                new List<ItemData>(),
                blueItems);

            archerLightConeSystem = GachaSystem.Create(GachaType.EventLightCone);
            archerLightConeSystem.LoadPools(
                goldAvatars, goldLightCones,
                new List<ItemData>(),
                archerLCGold,
                purpleAvatars, purpleLightCones,
                new List<ItemData>(),
                blueItems);

            saberAvatarSystem = GachaSystem.Create(GachaType.EventAvatar);
            saberAvatarSystem.LoadPools(
                goldAvatars, goldLightCones,
                celestialGoldRaw,
                saberAvatarGold,
                purpleAvatars, purpleLightCones,
                new List<ItemData>(),
                blueItems);

            saberLightConeSystem = GachaSystem.Create(GachaType.EventLightCone);
            saberLightConeSystem.LoadPools(
                goldAvatars, goldLightCones,
                new List<ItemData>(),
                saberLCGold,
                purpleAvatars, purpleLightCones,
                new List<ItemData>(),
                blueItems);
        }

        // ═══════════════════════════════════════════════════════════
        //  Event handlers
        // ═══════════════════════════════════════════════════════════

        private void Banner_Changed(object sender, RoutedEventArgs e)
        {
            if (initialising) return;

            if (rbOrdinary.IsChecked == true)
                currentSystem = ordinarySystem;
            else if (rbCyreneAvatar.IsChecked == true)
                currentSystem = cyreneSystem;
            else if (rbPhainonAvatar.IsChecked == true)
                currentSystem = phainonSystem;
            else if (rbCyreneLC.IsChecked == true)
                currentSystem = cyreneLightConeSystem;
            else if (rbPhainonLC.IsChecked == true)
                currentSystem = phainonLightConeSystem;
            else if (rbArcherAvatar.IsChecked == true)
                currentSystem = archerAvatarSystem;
            else if (rbArcherLC.IsChecked == true)
                currentSystem = archerLightConeSystem;
            else if (rbSaberAvatar.IsChecked == true)
                currentSystem = saberAvatarSystem;
            else if (rbSaberLC.IsChecked == true)
                currentSystem = saberLightConeSystem;

            currentResultIndex = -1; // reset to latest
            RefreshHistory();
            UpdatePityDisplay();
            UpdateStatistics();
            ShowLatestOrStoredResult();
        }

        private void BtnWarp1_Click(object sender, RoutedEventArgs e)
        {
            if (initialising) return;

            txtStatus.Text = "Pulling...";
            var item = currentSystem.Pull();
            currentResultIndex = currentSystem.History.Count - 1; // latest
            AfterPull();
        }

        private void BtnWarp10_Click(object sender, RoutedEventArgs e)
        {
            txtStatus.Text = "Pulling ×10...";
            var items = currentSystem.Pull10();
            // Show the last of the 10 pulls in the result card
            currentResultIndex = currentSystem.History.Count - 1;
            AfterPull();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            string bannerName = currentSystem.Type switch
            {
                GachaType.Ordinary       => "Ordinary",
                GachaType.EventAvatar    => currentSystem == cyreneSystem ? "Cyrene (Avatar)"
                                         : currentSystem == phainonSystem  ? "Phainon (Avatar)"
                                         : currentSystem == archerAvatarSystem   ? "Archer (Avatar)"
                                         : currentSystem == saberAvatarSystem    ? "Saber (Avatar)"
                                         : "Event Avatar",
                GachaType.EventLightCone => currentSystem == cyreneLightConeSystem  ? "Cyrene (LC)"
                                         : currentSystem == phainonLightConeSystem  ? "Phainon (LC)"
                                         : currentSystem == archerLightConeSystem   ? "Archer (LC)"
                                         : currentSystem == saberLightConeSystem    ? "Saber (LC)"
                                         : "Event Light Cone",
                _                        => "this"
            };

            var result = MessageBox.Show(
                $"Reset the {bannerName} banner? All pull history and pity for this banner will be lost.",
                "Reset Banner",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.OK)
                return;

            currentSystem.Reset();
            currentResultIndex = -1;
            ClearResultCard();
            RefreshHistory();
            UpdatePityDisplay();
            UpdateStatistics();
            txtStatus.Text = $"{bannerName} banner reset";
        }

        private void BtnPrevResult_Click(object sender, RoutedEventArgs e)
        {
            if (currentSystem.History.Count == 0)
                return;

            currentResultIndex--;
            if (currentResultIndex < 0)
                currentResultIndex = currentSystem.History.Count - 1; // wrap to oldest

            ShowResultAtIndex(currentResultIndex);
        }

        private void BtnNextResult_Click(object sender, RoutedEventArgs e)
        {
            if (currentSystem.History.Count == 0)
                return;

            currentResultIndex++;
            if (currentResultIndex >= currentSystem.History.Count)
                currentResultIndex = 0; // wrap to newest

            ShowResultAtIndex(currentResultIndex);
        }

        // ═══════════════════════════════════════════════════════════
        //  UI refresh helpers
        // ═══════════════════════════════════════════════════════════

        private void AfterPull()
        {
            RefreshHistory();
            UpdatePityDisplay();
            UpdateStatistics();
            ShowLatestOrStoredResult();
            txtStatus.Text = "Ready";
        }

        private void UpdatePityDisplay()
        {
            // Gold pity + guarantee
            txtGoldPity.Text = currentSystem.NonGoldGachaCount.ToString();
            txtGoldGuarantee.Text = currentSystem.IsGuaranteed
                ? "✅ Guaranteed"
                : "Not Guaranteed";
            txtGoldGuarantee.Foreground = currentSystem.IsGuaranteed
                ? new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00))
                : new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0));

            // Purple pity + guarantee
            txtPurplePity.Text = currentSystem.NonPurpleGachaCount.ToString();
            txtPurpleGuarantee.Text = currentSystem.IsPurpleGuaranteed
                ? "✅ Guaranteed"
                : "Not Guaranteed";
            txtPurpleGuarantee.Foreground = currentSystem.IsPurpleGuaranteed
                ? new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00))
                : new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0));
        }

        private void UpdateStatistics()
        {
            int total = currentSystem.TotalPulls;
            txtTotalPulls.Text = $"Total Pulls: {total}";

            if (total == 0)
            {
                txtGoldCount.Text = "0";
                txtGoldRate.Text = "  (—)";
                txtPurpleCount.Text = "0";
                txtPurpleRate.Text = "  (—)";
                txtBlueCount.Text = "0";
                txtBlueRate.Text = "  (—)";
            }
            else
            {
                int gold = currentSystem.GoldCount;
                int purple = currentSystem.PurpleCount;
                int blue = currentSystem.BlueCount;

                txtGoldCount.Text = gold.ToString();
                txtGoldRate.Text = $"  ({gold * 100.0 / total:F1}%)";
                txtPurpleCount.Text = purple.ToString();
                txtPurpleRate.Text = $"  ({purple * 100.0 / total:F1}%)";
                txtBlueCount.Text = blue.ToString();
                txtBlueRate.Text = $"  ({blue * 100.0 / total:F1}%)";
            }
        }

        private void RefreshHistory()
        {
            historyItems.Clear();
            var history = currentSystem.History;
            // Display newest first (highest index at top)
            for (int i = history.Count - 1; i >= 0; i--)
            {
                // Display index is 1‑based for the user
                historyItems.Add(HistoryItemDisplay.FromItemData(history[i], i + 1));
            }
        }

        private void ShowLatestOrStoredResult()
        {
            if (currentSystem.History.Count == 0)
            {
                ClearResultCard();
                return;
            }

            if (currentResultIndex < 0 || currentResultIndex >= currentSystem.History.Count)
                currentResultIndex = currentSystem.History.Count - 1;

            ShowResultAtIndex(currentResultIndex);
        }

        private void ShowResultAtIndex(int index)
        {
            if (index < 0 || index >= currentSystem.History.Count)
            {
                ClearResultCard();
                return;
            }

            currentResultIndex = index;
            var item = currentSystem.History[index];

            // Rarity stars with correct colour
            string stars = HistoryItemDisplay.FromItemData(item, 0).RarityStars;
            txtResultRarity.Text = stars;
            txtResultRarity.Foreground = RarityToBrush(item.Rarity);

            txtResultName.Text = item.Name;
            txtResultType.Text = item.Type == ItemType.Avatar ? "Avatar" : "Light Cone";
            txtResultPath.Text = PathToLabel(item.Path);

            // Element — coloured per element type, or "—" for Light Cones
            if (item.Type == ItemType.LightCone || item.ElementType == ElementType.Unknown)
            {
                txtResultElement.Text = "—";
                txtResultElement.Foreground = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0));
                dotElement.Visibility = Visibility.Collapsed;
                txtResultElement.Visibility = Visibility.Visible;
            }
            else
            {
                txtResultElement.Text = FormatElement(item.ElementType);
                txtResultElement.Foreground = ElementToBrush(item.ElementType);
                dotElement.Visibility = Visibility.Visible;
                txtResultElement.Visibility = Visibility.Visible;
            }

            txtResultPullNum.Text = $"Pull #{index + 1} of {currentSystem.History.Count}";
            txtResultIndex.Text = $"Pull #{index + 1} of {currentSystem.History.Count}";

            // Rarity‑coloured card border
            resultCardBorder.BorderBrush = RarityToBrush(item.Rarity);
        }

        private void ClearResultCard()
        {
            txtResultRarity.Text = "";
            txtResultName.Text = "Pull to begin";
            txtResultType.Text = "";
            txtResultPath.Text = "";
            txtResultElement.Text = "";
            txtResultPullNum.Text = "";
            txtResultIndex.Text = "";
            resultCardBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x6E));
            dotElement.Visibility = Visibility.Visible;
            txtResultElement.Visibility = Visibility.Visible;
        }

        // ═══════════════════════════════════════════════════════════
        //  Tiny formatting helpers
        // ═══════════════════════════════════════════════════════════

        private static SolidColorBrush RarityToBrush(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Gold   => new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)),
                ItemRarity.Purple => new SolidColorBrush(Color.FromRgb(0xC7, 0x7D, 0xFF)),
                ItemRarity.Blue   => new SolidColorBrush(Color.FromRgb(0x60, 0x90, 0xFF)),
                _                 => new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0))
            };
        }

        private static SolidColorBrush ElementToBrush(ElementType element)
        {
            return element switch
            {
                ElementType.Physical  => new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0)),
                ElementType.Fire      => new SolidColorBrush(Color.FromRgb(0xFF, 0x44, 0x44)),
                ElementType.Ice       => new SolidColorBrush(Color.FromRgb(0x44, 0x99, 0xFF)),
                ElementType.Lightning => new SolidColorBrush(Color.FromRgb(0xDD, 0x77, 0xDD)),
                ElementType.Wind      => new SolidColorBrush(Color.FromRgb(0x44, 0xCC, 0x44)),
                ElementType.Quantum   => new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0xCC)),
                ElementType.Imaginary => new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0x44)),
                _                     => new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0))
            };
        }

        private static string PathToLabel(PathType path)
        {
            return path switch
            {
                PathType.Destruction  => "Destruction",
                PathType.TheHunt      => "The Hunt",
                PathType.Erudition    => "Erudition",
                PathType.Harmony      => "Harmony",
                PathType.Nihility     => "Nihility",
                PathType.Preservation => "Preservation",
                PathType.Abundance    => "Abundance",
                PathType.Remembrance       => "Remembrance",
                PathType.Elation      => "Elation",
                _                     => "—"
            };
        }

        private static string FormatElement(ElementType element)
        {
            return element switch
            {
                ElementType.Physical  => "Physical",
                ElementType.Fire      => "Fire",
                ElementType.Ice       => "Ice",
                ElementType.Lightning => "Lightning",
                ElementType.Wind      => "Wind",
                ElementType.Quantum   => "Quantum",
                ElementType.Imaginary => "Imaginary",
                _                     => "—"
            };
        }
    }
}
