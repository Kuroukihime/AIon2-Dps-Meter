using AionDpsMeter.Services.Services.Session;
using AionDpsMeter.Services.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AionDpsMeter.UI.ViewModels.History
{
    public sealed partial class HistoryViewModel : ViewModelBase
    {
        private readonly CombatSessionManager _sessionManager;
        private const int DefaultPageSize = 100;

        [ObservableProperty]
        private ObservableCollection<HistoryEntryViewModel> _sessions = [];

        [ObservableProperty]
        private DateTime? _dateFrom;

        [ObservableProperty]
        private DateTime? _dateTo;

        [ObservableProperty]
        private string _bossName = string.Empty;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _pageSize = DefaultPageSize;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private string _summaryText = "Total 0, shown 0";

        public bool HasSessions => Sessions.Count > 0;
        public bool CanGoPrev => CurrentPage > 1;
        public bool CanGoNext => CurrentPage * PageSize < TotalCount;

        public HistoryViewModel(CombatSessionManager sessionManager, IAppSettingsService settingsService)
        {
            _sessionManager = sessionManager;

            var now = DateTime.Now;
            DateFrom = now.AddDays(-1).Date;
            DateTo = now.AddMinutes(1);

            LoadPage(1);
        }

        [RelayCommand]
        private void Search()
        {
            LoadPage(1);
        }

        [RelayCommand]
        private void ResetFilters()
        {
            var now = DateTime.Now;
            DateFrom = now.AddDays(-1).Date;
            DateTo = now.AddMinutes(1);
            BossName = string.Empty;
            LoadPage(1);
        }

        [RelayCommand(CanExecute = nameof(CanGoPrev))]
        private void PrevPage()
        {
            if (!CanGoPrev) return;
            LoadPage(CurrentPage - 1);
        }

        [RelayCommand(CanExecute = nameof(CanGoNext))]
        private void NextPage()
        {
            if (!CanGoNext) return;
            LoadPage(CurrentPage + 1);
        }

        partial void OnCurrentPageChanged(int value)
        {
            PrevPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
        }

        partial void OnTotalCountChanged(int value)
        {
            PrevPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
        }

        private void LoadPage(int page)
        {
            var query = new HistorySessionQuery
            {
                DateFrom = DateFrom,
                DateTo = NormalizeDateTo(DateTo),
                BossNameContains = string.IsNullOrWhiteSpace(BossName) ? null : BossName.Trim(),
                PageNumber = Math.Max(1, page),
                PageSize = PageSize,
            };

            var result = _sessionManager.GetHistoryPage(query);

            Sessions = new ObservableCollection<HistoryEntryViewModel>(
                result.Items.Select(s => new HistoryEntryViewModel(s)));

            TotalCount = result.TotalCount;
            CurrentPage = result.PageNumber;

            int shownStart = TotalCount == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
            int shownEnd = shownStart == 0 ? 0 : shownStart + Sessions.Count - 1;
            SummaryText = $"Total {TotalCount}, shown {shownStart}-{shownEnd}";
        }

        private static DateTime? NormalizeDateTo(DateTime? value)
        {
            if (value is null) return null;

            var dt = value.Value;
            if (dt.TimeOfDay == TimeSpan.Zero)
                return dt.Date.AddDays(1).AddTicks(-1);

            return dt;
        }
    }
}
