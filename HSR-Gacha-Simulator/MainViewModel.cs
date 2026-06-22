using System.Collections.ObjectModel;
using System.ComponentModel;
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

        // ── Current system ──────────────────────────────────────────
        private GachaSystem _currentSystem = null!;

        // ── History ─────────────────────────────────────────────────
        public ObservableCollection<HistoryItemDisplay> HistoryItems { get; } = new();

        private int _currentResultIndex = -1;
        public int CurrentResultIndex
        {
            get => _currentResultIndex;
            set { _currentResultIndex = value; OnPropertyChanged(); }
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
        //  Public methods — called from code-behind
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Switch to a different banner system.</summary>
        public void SwitchBanner(GachaSystem system)
        {
            _currentSystem = system;
            CurrentResultIndex = -1;
            ReloadAllHistory();
            RefreshPity();
            RefreshStatistics();
            ShowLatestOrStoredResult();
            StatusText = L10n.Get("ui.status.ready");
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
            if (_currentSystem.History.Count == 0)
                return;

            int idx = CurrentResultIndex - 1;
            if (idx < 0)
                idx = _currentSystem.History.Count - 1;

            ShowResult(idx);
        }

        /// <summary>Navigate to next result in history.</summary>
        public void NavigateNext()
        {
            if (_currentSystem.History.Count == 0)
                return;

            int idx = CurrentResultIndex + 1;
            if (idx >= _currentSystem.History.Count)
                idx = 0;

            ShowResult(idx);
        }

        /// <summary>Called after a banner reset.</summary>
        public void ResetBanner(string bannerName)
        {
            CurrentResultIndex = -1;
            ClearResult();
            ReloadAllHistory();
            RefreshPity();
            RefreshStatistics();
            StatusText = L10n.Get("ui.status.banner_reset", bannerName);
        }

        // ═══════════════════════════════════════════════════════════════
        //  History helpers
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Append the most recent <paramref name="count"/> pulls to the top of the list.</summary>
        public void AppendNewHistoryItems(int count)
        {
            var history = _currentSystem.History;
            // 'count' new items were added to the end of History
            for (int i = history.Count - count; i < history.Count; i++)
            {
                HistoryItems.Insert(0, HistoryItemDisplay.FromItemData(history[i], i + 1));
            }
        }

        /// <summary>Clear and reload the entire history list.</summary>
        public void ReloadAllHistory()
        {
            HistoryItems.Clear();
            var history = _currentSystem.History;
            // Display newest first (highest index at top)
            for (int i = history.Count - 1; i >= 0; i--)
            {
                HistoryItems.Add(HistoryItemDisplay.FromItemData(history[i], i + 1));
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Pity display
        // ═══════════════════════════════════════════════════════════════

        public void RefreshPity()
        {
            var sys = _currentSystem;

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
            var sys = _currentSystem;
            int total = sys.TotalPulls;
            TotalPulls = L10n.Get("ui.stats.total_pulls", total);

            if (total == 0)
            {
                GoldCount = "0"; GoldRate = "  (—)";
                PurpleCount = "0"; PurpleRate = "  (—)";
                BlueCount = "0"; BlueRate = "  (—)";
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
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Result card
        // ═══════════════════════════════════════════════════════════════

        public void ShowResult(int index)
        {
            var sys = _currentSystem;
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
            if (_currentSystem.History.Count == 0)
            {
                ClearResult();
                return;
            }

            if (CurrentResultIndex < 0 || CurrentResultIndex >= _currentSystem.History.Count)
                CurrentResultIndex = _currentSystem.History.Count - 1;

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
}
