using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using HSR_Gacha_Simulator.Services;

namespace HSR_Gacha_Simulator.Markup
{
    /// <summary>
    /// WPF markup extension that returns a live <see cref="Binding"/> to
    /// <c>LocalizationService.Current[key]</c> so text updates automatically
    /// when <c>CurrentLanguage</c> changes.
    /// </summary>
    /// <remarks>
    /// Usage in XAML:
    /// <code>{markup:Loc ui.button.warp_x1}</code>
    /// or
    /// <code>{markup:Loc Key=ui.button.warp_x1}</code>
    /// </remarks>
    [MarkupExtensionReturnType(typeof(object))]
    public class LocExtension : MarkupExtension
    {
        /// <summary>
        /// Parameterless constructor — the <c>Key</c> property must be set.
        /// </summary>
        public LocExtension() { }

        /// <summary>
        /// Single-argument constructor — accepts the localisation key directly.
        /// </summary>
        public LocExtension(string key)
        {
            Key = key;
        }

        /// <summary>
        /// The localisation key to look up (e.g. "ui.button.warp_x1").
        /// </summary>
        [ConstructorArgument("key")]
        public string? Key { get; set; }

        /// <summary>
        /// Provides the value for the target property.
        /// If the target is a <see cref="DependencyProperty"/> on a
        /// <see cref="DependencyObject"/>, returns a live <see cref="Binding"/>
        /// so language changes propagate automatically.
        /// Otherwise returns the translated string directly.
        /// </summary>
        public override object? ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Key))
                return Key ?? string.Empty;

            // When the target is a DependencyProperty on a DependencyObject,
            // return a Binding so live language switching works.
            if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget pvt &&
                pvt.TargetObject is DependencyObject &&
                pvt.TargetProperty is DependencyProperty)
            {
                var binding = new Binding
                {
                    Path = new PropertyPath($"[{Key}]"),
                    Source = LocalizationService.Current,
                    Mode = BindingMode.OneWay
                };
                return binding.ProvideValue(serviceProvider);
            }

            // Fallback for non-DP targets (e.g. inside templates/styles):
            // return the string directly.
            return LocalizationService.Current[Key];
        }
    }
}
