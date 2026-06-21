using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HSR_Gacha_Simulator
{
    /// <summary>
    /// Lightweight display wrapper used by the history <see cref="System.Windows.Controls.ListView"/>.
    /// Pre‑formats rarity stars, type labels, and path labels so the XAML can bind directly.
    /// Implements <see cref="INotifyPropertyChanged"/> so the view refreshes when the source list is rebuilt.
    /// </summary>
    public class HistoryItemDisplay : INotifyPropertyChanged
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
            set { _index = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string RarityStars
        {
            get => _rarityStars;
            set { _rarityStars = value; OnPropertyChanged(); }
        }

        public ItemRarity Rarity
        {
            get => _rarity;
            set { _rarity = value; OnPropertyChanged(); }
        }

        public string TypeLabel
        {
            get => _typeLabel;
            set { _typeLabel = value; OnPropertyChanged(); }
        }

        public string PathLabel
        {
            get => _pathLabel;
            set { _pathLabel = value; OnPropertyChanged(); }
        }

        public ElementType ElementType
        {
            get => _elementType;
            set { _elementType = value; OnPropertyChanged(); }
        }

        public string ElementLabel
        {
            get => _elementLabel;
            set { _elementLabel = value; OnPropertyChanged(); }
        }

        // ── Factory ─────────────────────────────────────────────

        public static HistoryItemDisplay FromItemData(ItemData item, int index)
        {
            return new HistoryItemDisplay
            {
                Index        = index,
                Name         = item.Name,
                RarityStars  = RarityToStars(item.Rarity),
                Rarity       = item.Rarity,
                TypeLabel    = item.Type == ItemType.Avatar ? "Avatar" : "L.Cone",
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

        private static string FormatPath(PathType path)
        {
            return path switch
            {
                PathType.Destruction  => "Destruction",
                PathType.TheHunt      => "The Hunt",
                PathType.Erudition    => "Erudition",
                PathType.Harmony      => "Harmony",
                PathType.Nihility     => "Nihility",
                PathType.Preservation => "Preserv.",
                PathType.Abundance    => "Abundance",
                PathType.Memory       => "Memory",
                PathType.Elation      => "Elation",
                _                     => "—"
            };
        }

        private static string FormatElement(ElementType element, ItemType type)
        {
            if (type == ItemType.LightCone || element == ElementType.Unknown)
                return "—";

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

        // ── INotifyPropertyChanged ──────────────────────────────

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
