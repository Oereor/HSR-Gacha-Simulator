using System.Windows;
using System.Windows.Controls;
using HSR_Gacha_Simulator.Services;
using HSR_Gacha_Simulator.ViewModels;

namespace HSR_Gacha_Simulator.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly ILocalizationService _l10n;

        public MainWindow(MainViewModel viewModel, ILocalizationService localizationService)
        {
            _viewModel = viewModel;
            _l10n = localizationService;

            InitializeComponent();

            DataContext = _viewModel;
            lvHistory.ItemsSource = _viewModel.HistoryPanel.HistoryItems;

            try
            {
                _viewModel.InitializeSystems();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    _l10n.Get("dialog.error.init_failed", ex.Message),
                    _l10n.Get("dialog.error.title"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _viewModel.StatusText = _l10n.Get("ui.status.init_failed");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Scroll handlers
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Suppress WPF auto-scroll when ListBox selection changes.</summary>
        private void BannerListBox_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

        private void BannerScrollLeft_Click(object sender, RoutedEventArgs e)
        {
            BannerScrollViewer.ScrollToHorizontalOffset(
                BannerScrollViewer.HorizontalOffset - 200);
        }

        private void BannerScrollRight_Click(object sender, RoutedEventArgs e)
        {
            BannerScrollViewer.ScrollToHorizontalOffset(
                BannerScrollViewer.HorizontalOffset + 200);
        }

        private void BannerScroll_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            // Redirect vertical mouse-wheel to horizontal scroll
            BannerScrollViewer.ScrollToHorizontalOffset(
                BannerScrollViewer.HorizontalOffset - e.Delta / 3);
            e.Handled = true;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Event handlers
        // ═══════════════════════════════════════════════════════════════

        private void BtnWarp1_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IsLoading) return;

            _viewModel.StatusText = _l10n.Get("ui.status.pulling");
            _viewModel.Pull(1);
        }

        private void BtnWarp10_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IsLoading) return;

            _viewModel.StatusText = _l10n.Get("ui.status.pulling_x10");
            _viewModel.Pull(10);
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            string bannerName = _viewModel.SelectedBanner?.DisplayName ?? _l10n.Get("ui.banner.ordinary");

            var result = MessageBox.Show(
                _l10n.Get("dialog.reset_banner.message", bannerName),
                _l10n.Get("dialog.reset_banner.title"),
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.OK)
                return;

            _viewModel.ResetCurrentBanner();
        }

        private void BtnPrevResult_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.NavigatePrev();
        }

        private void BtnNextResult_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.NavigateNext();
        }
    }
}
