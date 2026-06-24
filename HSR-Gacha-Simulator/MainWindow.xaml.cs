using System.Windows;
using System.Windows.Controls;

namespace HSR_Gacha_Simulator
{
    public partial class MainWindow : Window
    {
        // ── ViewModel ───────────────────────────────────────────────
        private readonly MainViewModel viewModel = new();

        // Prevents event handlers from running before initialisation is complete.
        private bool initialising;

        // ── Localisation shorthand ──────────────────────────────────
        private static LocalizationService L10n => LocalizationService.Instance;

        public MainWindow()
        {
            initialising = true;
            InitializeComponent();

            // Set up data binding
            DataContext = viewModel;
            lvHistory.ItemsSource = viewModel.HistoryItems;

            try
            {
                viewModel.InitializeSystems();
                initialising = false;
            }
            catch (Exception ex)
            {
                initialising = false;
                MessageBox.Show(
                    L10n.Get("dialog.error.init_failed", ex.Message),
                    L10n.Get("dialog.error.title"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                viewModel.StatusText = L10n.Get("ui.status.init_failed");
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
            if (initialising || viewModel.IsLoading) return;

            viewModel.StatusText = L10n.Get("ui.status.pulling");
            viewModel.Pull(1);
        }

        private void BtnWarp10_Click(object sender, RoutedEventArgs e)
        {
            if (initialising || viewModel.IsLoading) return;

            viewModel.StatusText = L10n.Get("ui.status.pulling_x10");
            viewModel.Pull(10);
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            string bannerName = viewModel.SelectedBanner?.DisplayName ?? L10n.Get("ui.banner.ordinary");

            var result = MessageBox.Show(
                L10n.Get("dialog.reset_banner.message", bannerName),
                L10n.Get("dialog.reset_banner.title"),
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.OK)
                return;

            viewModel.ResetCurrentBanner();
        }

        private void BtnPrevResult_Click(object sender, RoutedEventArgs e)
        {
            viewModel.NavigatePrev();
        }

        private void BtnNextResult_Click(object sender, RoutedEventArgs e)
        {
            viewModel.NavigateNext();
        }
    }
}
