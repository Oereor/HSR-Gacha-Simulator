using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HSR_Gacha_Simulator
{
    /// <summary>Maps <see cref="ItemRarity"/> to a <see cref="Brush"/> for the result card border.</summary>
    [ValueConversion(typeof(ItemRarity), typeof(Brush))]
    public class RarityToBorderBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush GoldBrush   = new(Color.FromRgb(0xFF, 0xD7, 0x00));
        private static readonly SolidColorBrush PurpleBrush = new(Color.FromRgb(0xC7, 0x7D, 0xFF));
        private static readonly SolidColorBrush BlueBrush   = new(Color.FromRgb(0x60, 0x90, 0xFF));
        private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(0x3A, 0x3A, 0x6E));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ItemRarity rarity)
            {
                return rarity switch
                {
                    ItemRarity.Gold   => GoldBrush,
                    ItemRarity.Purple => PurpleBrush,
                    ItemRarity.Blue   => BlueBrush,
                    _                 => DefaultBrush
                };
            }
            return DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Maps <see cref="ItemRarity"/> to a <see cref="Brush"/> for the rarity stars text.</summary>
    [ValueConversion(typeof(ItemRarity), typeof(Brush))]
    public class RarityToForegroundConverter : IValueConverter
    {
        private static readonly SolidColorBrush GoldBrush   = new(Color.FromRgb(0xFF, 0xD7, 0x00));
        private static readonly SolidColorBrush PurpleBrush = new(Color.FromRgb(0xC7, 0x7D, 0xFF));
        private static readonly SolidColorBrush BlueBrush   = new(Color.FromRgb(0x60, 0x90, 0xFF));
        private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(0xE0, 0xE0, 0xE0));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ItemRarity rarity)
            {
                return rarity switch
                {
                    ItemRarity.Gold   => GoldBrush,
                    ItemRarity.Purple => PurpleBrush,
                    ItemRarity.Blue   => BlueBrush,
                    _                 => DefaultBrush
                };
            }
            return DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
