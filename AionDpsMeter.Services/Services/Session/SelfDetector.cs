using AionDpsMeter.Services.Services.Entity;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.Services.Session
{
    /// <summary>
    /// Finds the user's row before the game sends the user's own player info: the cooldown message only lists the user's
    /// skills, and a skill on cooldown can't be cast again, so the one player hitting with that skill as its cooldown
    /// starts is the user. Not thread-safe; called under the session manager's lock.
    /// </summary>
    internal sealed class SelfDetector(EntityTracker entityTracker, ILogger logger)
    {
        private static readonly TimeSpan MatchWindow = TimeSpan.FromSeconds(1.5);
        private const int MinVotes = 3;
        private const int MinLead = 3;

        private readonly Queue<(int SkillId, DateTime At)> _cooldowns = new();
        private readonly List<(int ActorId, int SkillId, DateTime At)> _hits = new();
        private readonly Dictionary<int, int> _votes = new();

        public void OnCooldownStarted(int skillId, DateTime at)
        {
            if (skillId <= 0 || entityTracker.IsUserConfirmed) return;
            _cooldowns.Enqueue((skillId, at));
            Settle(at);
        }

        public void OnHit(int actorId, int skillId, DateTime at)
        {
            if (skillId <= 0 || entityTracker.IsUserConfirmed) return;
            _hits.Add((actorId, skillId, at));
            Settle(at);
        }

        public void Reset()
        {
            _cooldowns.Clear();
            _hits.Clear();
            _votes.Clear();
        }

        // A cooldown is judged once its window has passed, so hits arriving just before or after it are all seen;
        // each cooldown votes at most once, and only when exactly one player hit with that skill.
        private void Settle(DateTime now)
        {
            while (_cooldowns.Count > 0 && now - _cooldowns.Peek().At >= MatchWindow)
            {
                var (skillId, at) = _cooldowns.Dequeue();
                var actors = _hits
                    .Where(h => h.SkillId == skillId && (h.At - at).Duration() <= MatchWindow)
                    .Select(h => h.ActorId)
                    .Distinct()
                    .ToList();

                if (actors.Count == 1) Vote(actors[0]);
            }

            _hits.RemoveAll(h => now - h.At > MatchWindow + MatchWindow);
        }

        private void Vote(int actorId)
        {
            _votes[actorId] = _votes.GetValueOrDefault(actorId) + 1;

            var ranked = _votes.OrderByDescending(v => v.Value).ToList();
            int top = ranked[0].Value;
            int second = ranked.Count > 1 ? ranked[1].Value : 0;
            if (top < MinVotes || top - second < MinLead) return;

            int userId = ranked[0].Key;
            if (entityTracker.MarkUserSession(userId))
                logger.LogInformation("Self-detection: session {SessionId} is the user ({Votes} matching casts)", userId, top);
            _votes.Clear();
        }
    }
}
