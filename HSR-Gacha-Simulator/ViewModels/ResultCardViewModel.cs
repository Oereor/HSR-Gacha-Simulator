using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HSR_Gacha_Simulator.Converters;
using HSR_Gacha_Simulator.Models;
using HSR_Gacha_Simulator.Services;

namespace HSR_Gacha_Simulator.ViewModels
{
    public class ResultCardViewModel : BaseViewModel
    {
        private static readonly SolidColorBrush DimBrush = new(Color.FromRgb(0xC0, 0xC0, 0xC0));
        private static readonly SolidColorBrush DefaultCardBorderBrush = new(Color.FromRgb(0x3A, 0x3A, 0x6E));

        private readonly ILocalizationService _l10n;
        private readonly IIconService _iconService;

        // ── Result Card ─────────────────────────────────────────────
        private string _resultRarity = "";
        public string ResultRarity { get => _resultRarity; set => SetProperty(ref _resultRarity, value); }

        private Brush _resultRarityBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
        public Brush ResultRarityBrush { get => _resultRarityBrush; set => SetProperty(ref _resultRarityBrush, value); }

        private string _resultName = "";
        public string ResultName { get => _resultName; set => SetProperty(ref _resultName, value); }

        private string _resultType = "";
        public string ResultType { get => _resultType; set => SetProperty(ref _resultType, value); }

        private string _resultPath = "";
        public string ResultPath { get => _resultPath; set => SetProperty(ref _resultPath, value); }

        private string _resultElement = "";
        public string ResultElement { get => _resultElement; set => SetProperty(ref _resultElement, value); }

        private Brush _resultElementBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0));
        public Brush ResultElementBrush { get => _resultElementBrush; set => SetProperty(ref _resultElementBrush, value); }

        private ImageSource? _resultPathIcon;
        public ImageSource? ResultPathIcon { get => _resultPathIcon; set => SetProperty(ref _resultPathIcon, value); }

        private Visibility _resultPathIconVisibility = Visibility.Collapsed;
        public Visibility ResultPathIconVisibility { get => _resultPathIconVisibility; set => SetProperty(ref _resultPathIconVisibility, value); }

        private ImageSource? _resultElementIcon;
        public ImageSource? ResultElementIcon { get => _resultElementIcon; set => SetProperty(ref _resultElementIcon, value); }

        private Visibility _resultElementIconVisibility = Visibility.Collapsed;
        public Visibility ResultElementIconVisibility { get => _resultElementIconVisibility; set => SetProperty(ref _resultElementIconVisibility, value); }

        private Brush _resultCardBorderBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x6E));
        public Brush ResultCardBorderBrush { get => _resultCardBorderBrush; set => SetProperty(ref _resultCardBorderBrush, value); }

        private string _resultIndexText = "";
        public string ResultIndexText { get => _resultIndexText; set => SetProperty(ref _resultIndexText, value); }

        private Visibility _dotElementVisibility = Visibility.Visible;
        public Visibility DotElementVisibility { get => _dotElementVisibility; set => SetProperty(ref _dotElementVisibility, value); }

        private Visibility _resultElementTextVisibility = Visibility.Visible;
        public Visibility ResultElementTextVisibility { get => _resultElementTextVisibility; set => SetProperty(ref _resultElementTextVisibility, value); }

        public ResultCardViewModel(ILocalizationService localizationService, IIconService iconService)
        {
            _l10n = localizationService;
            _iconService = iconService;
        }

        public void ShowResult(GachaSystem system, int index)
        {
            if (system.History.Count == 0 || index < 0 || index >= system.History.Count)
            {
                ClearResult();
                return;
            }

            var item = system.History[index];

            // Rarity
            ResultRarity = HistoryItemDisplay.FromItemData(item, 0).RarityStars;
            ResultRarityBrush = RarityConverters.GetForegroundBrush(item.Rarity);

            // Name
            ResultName = _l10n.GetItemName(item.Name);

            // Type
            ResultType = item.Type == ItemType.Avatar
                ? _l10n.Get("ui.result.type.avatar")
                : _l10n.Get("ui.result.type.lightcone");

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
            ResultPathIcon = _iconService.LoadOrNull($"Path_{item.Path}.png");
            ResultPathIconVisibility = ResultPathIcon != null ? Visibility.Visible : Visibility.Collapsed;

            if (item.Type == ItemType.LightCone || item.ElementType == ElementType.Unknown)
            {
                ResultElementIcon = null;
                ResultElementIconVisibility = Visibility.Collapsed;
            }
            else
            {
                ResultElementIcon = _iconService.LoadOrNull($"Element_{item.ElementType}.png");
                ResultElementIconVisibility = ResultElementIcon != null ? Visibility.Visible : Visibility.Collapsed;
            }

            // Pull number (displayed between nav buttons)
            ResultIndexText = _l10n.Get("ui.result.pull_number", index + 1, system.History.Count);

            // Border
            ResultCardBorderBrush = RarityConverters.GetBorderBrush(item.Rarity);

            DotElementVisibility = Visibility.Visible;
            ResultElementTextVisibility = Visibility.Visible;
        }

        public void ClearResult()
        {
            ResultRarity = "";
            ResultName = _l10n.Get("ui.result.default");
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

        public int NavigatePrev(int currentIndex, int historyCount)
        {
            if (historyCount == 0)
                return currentIndex;

            int idx = currentIndex - 1;
            if (idx < 0)
                idx = historyCount - 1;
            return idx;
        }

        public int NavigateNext(int currentIndex, int historyCount)
        {
            if (historyCount == 0)
                return currentIndex;

            int idx = currentIndex + 1;
            if (idx >= historyCount)
                idx = 0;
            return idx;
        }
    }
}
