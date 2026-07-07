using HSR_Gacha_Simulator.Models;
using HSR_Gacha_Simulator.Services;

namespace HSR_Gacha_Simulator.ViewModels
{
    /// <summary>
    /// ViewModel for one selectable banner in the horizontal scroll strip.
    /// </summary>
    public class BannerViewModel : BaseViewModel
    {
        private bool _isSelected;

        /// <summary>The GachaSystem backing this banner.</summary>
        public GachaSystem System { get; }

        /// <summary>Stable key for localization, e.g. "cyrene_avatar".</summary>
        public string BannerKey { get; }

        /// <summary>English display title, e.g. "Cyrene (Avatar)" — fallback if loc key missing.</summary>
        public string BannerTitle { get; }

        /// <summary>Avatar or LightCone — determines probability model.</summary>
        public GachaType GachaType { get; }

        /// <summary>True when this banner is the currently active one.</summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>Localized display name for live language switching.</summary>
        public string DisplayName =>
            LocalizationService.Current[$"ui.banner.{BannerKey}"] is string loc && loc != $"ui.banner.{BannerKey}"
                ? loc
                : BannerTitle;

        public BannerViewModel(GachaSystem system, string bannerKey, string bannerTitle, GachaType gachaType)
        {
            System = system;
            BannerKey = bannerKey;
            BannerTitle = bannerTitle;
            GachaType = gachaType;
        }
    }
}
