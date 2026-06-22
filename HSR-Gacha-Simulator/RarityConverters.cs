using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HSR_Gacha_Simulator
{
    /// <summary>Static helpers for rarity → brush lookups.</summary>
    public static class RarityConverters
    {
        public static SolidColorBrush GoldBrush { get; } = new(Color.FromRgb(0xFF, 0xD7, 0x00));
        public static SolidColorBrush PurpleBrush { get; } = new(Color.FromRgb(0xC7, 0x7D, 0xFF));
        public static SolidColorBrush BlueBrush { get; } = new(Color.FromRgb(0x60, 0x90, 0xFF));
        public static SolidColorBrush DefaultForegroundBrush { get; } = new(Color.FromRgb(0xE0, 0xE0, 0xE0));
        public static SolidColorBrush DefaultBorderBrush { get; } = new(Color.FromRgb(0x3A, 0x3A, 0x6E));

        public static SolidColorBrush GetForegroundBrush(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Gold   => GoldBrush,
            ItemRarity.Purple => PurpleBrush,
            ItemRarity.Blue   => BlueBrush,
            _                 => DefaultForegroundBrush
        };

        public static SolidColorBrush GetBorderBrush(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Gold   => GoldBrush,
            ItemRarity.Purple => PurpleBrush,
            ItemRarity.Blue   => BlueBrush,
            _                 => DefaultBorderBrush
        };
    }

    /// <summary>Maps <see cref="ItemRarity"/> to a <see cref="Brush"/> for the result card border.</summary>
    [ValueConversion(typeof(ItemRarity), typeof(Brush))]
    public class RarityToBorderBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ItemRarity rarity)
                return RarityConverters.GetBorderBrush(rarity);
            return RarityConverters.DefaultBorderBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Maps <see cref="ItemRarity"/> to a <see cref="Brush"/> for the rarity stars text.</summary>
    [ValueConversion(typeof(ItemRarity), typeof(Brush))]
    public class RarityToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ItemRarity rarity)
                return RarityConverters.GetForegroundBrush(rarity);
            return RarityConverters.DefaultForegroundBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
