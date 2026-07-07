using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using HSR_Gacha_Simulator.Models;

namespace HSR_Gacha_Simulator.Converters
{
    /// <summary>
    /// Maps an <see cref="ElementType"/> enum value to a <see cref="SolidColorBrush"/>
    /// for displaying element text in the UI.
    ///
    /// Colors follow HSRʼs in‑game element colour scheme:
    ///   Physical  → Silver,   Fire → Red,        Ice → Blue,
    ///   Lightning → Pink,     Wind → Green,       Quantum → Deep Purple,
    ///   Imaginary → Yellow.   Unknown / Light Cone → semi‑transparent.
    /// </summary>
    [ValueConversion(typeof(ElementType), typeof(Brush))]
    public class ElementTypeToBrushConverter : IValueConverter
    {
        public static Brush GetBrush(ElementType element) => element switch
        {
            ElementType.Physical  => PhysicalBrush,
            ElementType.Fire      => FireBrush,
            ElementType.Ice       => IceBrush,
            ElementType.Lightning => LightningBrush,
            ElementType.Wind      => WindBrush,
            ElementType.Quantum   => QuantumBrush,
            ElementType.Imaginary => ImaginaryBrush,
            _                     => DefaultBrush
        };

        private static readonly Brush DefaultBrush = Brushes.Transparent;

        private static readonly Brush PhysicalBrush  = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0));
        private static readonly Brush FireBrush       = new SolidColorBrush(Color.FromRgb(0xFF, 0x44, 0x44));
        private static readonly Brush IceBrush        = new SolidColorBrush(Color.FromRgb(0x44, 0x99, 0xFF));
        private static readonly Brush LightningBrush = new SolidColorBrush(Color.FromRgb(0xDD, 0x77, 0xDD));
        private static readonly Brush WindBrush       = new SolidColorBrush(Color.FromRgb(0x44, 0xCC, 0x44));
        private static readonly Brush QuantumBrush    = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0xCC));
        private static readonly Brush ImaginaryBrush  = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0x44));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ElementType element)
                return GetBrush(element);
            return DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
