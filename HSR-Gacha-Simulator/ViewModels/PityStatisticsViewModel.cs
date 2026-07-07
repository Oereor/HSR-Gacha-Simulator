using System.Windows;
using System.Windows.Media;
using HSR_Gacha_Simulator.Models;
using HSR_Gacha_Simulator.Services;

namespace HSR_Gacha_Simulator.ViewModels
{
    public class PityStatisticsViewModel : BaseViewModel
    {
        private static readonly SolidColorBrush GoldBrush = new(Color.FromRgb(0xFF, 0xD7, 0x00));
        private static readonly SolidColorBrush DimBrush = new(Color.FromRgb(0xC0, 0xC0, 0xC0));

        private readonly ILocalizationService _l10n;

        // ── Pity: Gold ──────────────────────────────────────────────
        private string _goldPity = "0";
        public string GoldPity { get => _goldPity; set => SetProperty(ref _goldPity, value); }

        private string _goldGuarantee = "";
        public string GoldGuarantee { get => _goldGuarantee; set => SetProperty(ref _goldGuarantee, value); }

        private Brush _goldGuaranteeBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0));
        public Brush GoldGuaranteeBrush { get => _goldGuaranteeBrush; set => SetProperty(ref _goldGuaranteeBrush, value); }

        // ── Pity: Purple ────────────────────────────────────────────
        private string _purplePity = "0";
        public string PurplePity { get => _purplePity; set => SetProperty(ref _purplePity, value); }

        private string _purpleGuarantee = "";
        public string PurpleGuarantee { get => _purpleGuarantee; set => SetProperty(ref _purpleGuarantee, value); }

        private Brush _purpleGuaranteeBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0));
        public Brush PurpleGuaranteeBrush { get => _purpleGuaranteeBrush; set => SetProperty(ref _purpleGuaranteeBrush, value); }

        // ── Statistics ──────────────────────────────────────────────
        private string _totalPulls = "";
        public string TotalPulls { get => _totalPulls; set => SetProperty(ref _totalPulls, value); }

        private string _goldCount = "0";
        public string GoldCount { get => _goldCount; set => SetProperty(ref _goldCount, value); }

        private string _goldRate = "";
        public string GoldRate { get => _goldRate; set => SetProperty(ref _goldRate, value); }

        private string _purpleCount = "0";
        public string PurpleCount { get => _purpleCount; set => SetProperty(ref _purpleCount, value); }

        private string _purpleRate = "";
        public string PurpleRate { get => _purpleRate; set => SetProperty(ref _purpleRate, value); }

        private string _blueCount = "0";
        public string BlueCount { get => _blueCount; set => SetProperty(ref _blueCount, value); }

        private string _blueRate = "";
        public string BlueRate { get => _blueRate; set => SetProperty(ref _blueRate, value); }

        private string _missedGoldStats = "";
        public string MissedGoldStats { get => _missedGoldStats; set => SetProperty(ref _missedGoldStats, value); }

        private Visibility _missedStatsVisibility = Visibility.Collapsed;
        public Visibility MissedStatsVisibility { get => _missedStatsVisibility; set => SetProperty(ref _missedStatsVisibility, value); }

        public PityStatisticsViewModel(ILocalizationService localizationService)
        {
            _l10n = localizationService;
        }

        public void Refresh(GachaSystem system)
        {
            // ── Pity display ──────────────────────────────────────
            GoldPity = system.NonGoldGachaCount.ToString();
            GoldGuarantee = system.IsGuaranteed
                ? _l10n.Get("ui.pity.guaranteed")
                : _l10n.Get("ui.pity.not_guaranteed");
            GoldGuaranteeBrush = system.IsGuaranteed ? GoldBrush : DimBrush;

            PurplePity = system.NonPurpleGachaCount.ToString();
            PurpleGuarantee = system.IsPurpleGuaranteed
                ? _l10n.Get("ui.pity.guaranteed")
                : _l10n.Get("ui.pity.not_guaranteed");
            PurpleGuaranteeBrush = system.IsPurpleGuaranteed ? GoldBrush : DimBrush;

            // ── Statistics display ────────────────────────────────
            int total = system.TotalPulls;
            TotalPulls = _l10n.Get("ui.stats.total_pulls", total);

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
                int gold = system.GoldCount, purple = system.PurpleCount, blue = system.BlueCount;
                GoldCount = gold.ToString();
                GoldRate = $"  ({gold * 100.0 / total:F1}%)";
                PurpleCount = purple.ToString();
                PurpleRate = $"  ({purple * 100.0 / total:F1}%)";
                BlueCount = blue.ToString();
                BlueRate = $"  ({blue * 100.0 / total:F1}%)";

                if (system.HasEventItems && gold > 0)
                {
                    int missed = system.MissedGoldCount;
                    double rate = missed * 100.0 / gold;
                    MissedGoldStats = _l10n.Get("ui.stats.missed", missed, gold, rate);
                    MissedStatsVisibility = Visibility.Visible;
                }
                else
                {
                    MissedGoldStats = "";
                    MissedStatsVisibility = Visibility.Collapsed;
                }
            }
        }
    }
}
