using AionDpsMeter.Services.Services.Session;

namespace AionDpsMeter.UI.ViewModels.History
{
    public sealed class HistoryEntryViewModel : ViewModelBase
    {
        public HistorySessionListItem Session { get; }

        public HistoryEntryViewModel(HistorySessionListItem session)
        {
            Session = session;
        }

        public Guid SessionId => Session.SessionId;

        public string TargetName    => string.IsNullOrEmpty(Session.TargetName)
            ? $"Mob #{Session.TargetId}"
            : Session.TargetName;

        public string DateDisplay   => Session.SessionEnd.ToString("dd.MM  HH:mm:ss");
        public string Duration      => DamageFormatter.FormatDuration(Session.Duration);
        public bool   IsCompleted   => Session.State == SessionState.Completed;
        public string StateDisplay  => IsCompleted ? "?" : "?";
        public string StateColor    => IsCompleted ? "#888888" : "#4EC9B0";
        public int PlayerCount => Session.PlayerCount;

        public string TotalDamageDisplay => DamageFormatter.Format(Session.TotalDamage);
    }
}
