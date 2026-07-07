using System.Collections.ObjectModel;
using HSR_Gacha_Simulator.Models;
using HSR_Gacha_Simulator.Services;

namespace HSR_Gacha_Simulator.ViewModels
{
    public class HistoryPanelViewModel : BaseViewModel
    {
        private readonly ILocalizationService _l10n;

        public ObservableCollection<HistoryItemDisplay> HistoryItems { get; } = new();

        private int _currentResultIndex = -1;
        public int CurrentResultIndex
        {
            get => _currentResultIndex;
            set => SetProperty(ref _currentResultIndex, value);
        }

        public HistoryPanelViewModel(ILocalizationService localizationService)
        {
            _l10n = localizationService;
        }

        /// <summary>Append the most recent <paramref name="count"/> pulls to the top of the list.</summary>
        public void AppendNewHistoryItems(GachaSystem system, int count)
        {
            var history = system.History;
            for (int i = history.Count - count; i < history.Count; i++)
            {
                HistoryItems.Insert(0, HistoryItemDisplay.FromItemData(history[i], i + 1));
            }
        }

        /// <summary>Clear the history list.</summary>
        public void ClearHistory()
        {
            HistoryItems.Clear();
        }

        /// <summary>
        /// Asynchronously reloads the entire history list from the current
        /// <see cref="GachaSystem.History"/>.  The heavy work (creating
        /// <see cref="HistoryItemDisplay"/> objects) runs on a thread-pool
        /// thread so the UI stays responsive.  The final
        /// <see cref="ObservableCollection{T}"/> update is dispatched back
        /// to the UI thread.
        /// </summary>
        public async Task ReloadAllHistoryAsync(GachaSystem system)
        {
            if (system == null) return;

            // Capture the history snapshot on the UI thread before
            // dispatching to the thread pool.  This avoids cross-thread
            // access to GachaSystem.History (which is not thread-safe).
            var snapshot = system.History.ToArray();

            List<HistoryItemDisplay> newItems;

            // CPU-bound work — runs off the UI thread
            newItems = await Task.Run(() =>
            {
                var list = new List<HistoryItemDisplay>(snapshot.Length);
                // Newest first (same ordering as the original RefreshHistory)
                for (int i = snapshot.Length - 1; i >= 0; i--)
                {
                    list.Add(HistoryItemDisplay.FromItemData(snapshot[i], i + 1));
                }
                return list;
            });

            // UI-thread work
            HistoryItems.Clear();
            foreach (var item in newItems)
            {
                HistoryItems.Add(item);
            }
        }
    }
}
