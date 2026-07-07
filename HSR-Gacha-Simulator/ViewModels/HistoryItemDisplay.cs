using System.ComponentModel;
using System.Runtime.CompilerServices;
using HSR_Gacha_Simulator.Models;
using HSR_Gacha_Simulator.Services;

namespace HSR_Gacha_Simulator.ViewModels
{
    /// <summary>
    /// Lightweight display wrapper used by the history <see cref="System.Windows.Controls.ListView"/>.
    /// Pre‑formats rarity stars, type labels, and path labels so the XAML can bind directly.
    /// </summary>
    public class HistoryItemDisplay : BaseViewModel
    {
        private int _index;
        private string _name = "";
        private string _rarityStars = "";
        private ItemRarity _rarity;
        private string _typeLabel = "";
        private string _pathLabel = "";
        private ElementType _elementType;
        private string _elementLabel = "";

        public int Index
        {
            get => _index;
            set => SetProperty(ref _index, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string RarityStars
        {
            get => _rarityStars;
            set => SetProperty(ref _rarityStars, value);
        }

        public ItemRarity Rarity
        {
            get => _rarity;
            set => SetProperty(ref _rarity, value);
        }

        public string TypeLabel
        {
            get => _typeLabel;
            set => SetProperty(ref _typeLabel, value);
        }

        public string PathLabel
        {
            get => _pathLabel;
            set => SetProperty(ref _pathLabel, value);
        }

        public ElementType ElementType
        {
            get => _elementType;
            set => SetProperty(ref _elementType, value);
        }

        public string ElementLabel
        {
            get => _elementLabel;
            set => SetProperty(ref _elementLabel, value);
        }

        // ── Factory ─────────────────────────────────────────────

        public static HistoryItemDisplay FromItemData(ItemData item, int index)
        {
            var l10n = LocalizationService.Current;
            return new HistoryItemDisplay
            {
                Index        = index,
                Name         = l10n.GetItemName(item.Name),
                RarityStars  = RarityToStars(item.Rarity),
                Rarity       = item.Rarity,
                TypeLabel    = item.Type == ItemType.Avatar
                    ? l10n.Get("ui.history.type.avatar")
                    : l10n.Get("ui.history.type.lightcone_short"),
                PathLabel    = FormatPath(item.Path),
                ElementType  = item.ElementType,
                ElementLabel = FormatElement(item.ElementType, item.Type)
            };
        }

        // ── Formatters ──────────────────────────────────────────

        private static string RarityToStars(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Gold   => "★★★★★",
                ItemRarity.Purple => "★★★★",
                ItemRarity.Blue   => "★★★",
                _                 => "?"
            };
        }

        public static string FormatPath(PathType path)
        {
            if (path == PathType.Unknown)
                return "—";
            return LocalizationService.Current[$"path.{path}"];
        }

        public static string FormatElement(ElementType element, ItemType type)
        {
            if (type == ItemType.LightCone || element == ElementType.Unknown)
                return "—";

            return LocalizationService.Current[$"element.{element}"];
        }
    }
}
