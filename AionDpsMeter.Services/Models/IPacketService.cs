using AionDpsMeter.Core.Models;

namespace AionDpsMeter.Services.Models
{
    public interface IPacketService
    {
        event EventHandler<PlayerDamage>? DamageReceived;
        /// <summary>
        /// Fired when a new ping measurement is available.
        /// The event arg is ping in milliseconds.
        /// </summary>
        event EventHandler<int>? PingUpdated;

        /// <summary>
        /// Current ping in milliseconds. -1 if not yet measured.
        /// </summary>
        int CurrentPingMs { get; }
        void Start();
        void Stop();
        void Reset();
    }
}
