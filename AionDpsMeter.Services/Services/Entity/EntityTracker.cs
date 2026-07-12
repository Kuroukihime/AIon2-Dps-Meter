using AionDpsMeter.Core.Models;
using System.Diagnostics;

namespace AionDpsMeter.Services.Services.Entity
{
  
    public sealed class EntityTracker
    {
        // Fired when a summon is registered: (summonId, ownerId)
        public event Action<int, int>? SummonRegistered;

        public List<Player> PlayerEntities => sessionPlayers.Values.ToList();
        public List<Mob> TargetEntities => targetEntities.Values.ToList();
        public int PlayerEntityCount => sessionPlayers.Count;
        public int TargetEntityCount => targetEntities.Count;
        public int SummonCount => summons.Count;

        // Session id -> player. 
        private readonly Dictionary<int, Player> sessionPlayers = [];

        // Global id -> player.
        private readonly Dictionary<int, Player> globalPlayers = [];

        private readonly Dictionary<int, Mob> targetEntities = [];
        private readonly Dictionary<int, int> summons = []; // summonId -> ownerId

        private string currentUserName = string.Empty;

        // ----- Targets (mobs) --------------------------------------------------

        public Mob GetOrCreateTargetEntity(int entityId)
        {
            if (targetEntities.TryGetValue(entityId, out var entity)) return entity;

            entity = new Mob { Id = entityId };
            targetEntities[entityId] = entity;
            return entity;
        }

        public bool UpdateTargetEntityHpCurrent(int entityId, int hpCurrent)
        {
            if (!targetEntities.TryGetValue(entityId, out var entity)) return false;

            entity.HpCurrent = hpCurrent;
            if (entity.HpTotal > 0 && entity.HpCurrent > entity.HpTotal + 10_000_000)
            {
                entity.HpTotal = entity.HpCurrent;
            }
            return true;
        }

        public void CreateOrUpdateTargetEntity(int entityId, int mobCode, int hpTotal = 0)
        {
            if (targetEntities.TryGetValue(entityId, out var entity))
            {
                entity.MobCode = mobCode;
                if (hpTotal > 0) entity.HpTotal = hpTotal;
                return;
            }

            targetEntities[entityId] = new Mob
            {
                Id = entityId,
                MobCode = mobCode,
                HpTotal = hpTotal,
            };
        }

        public Core.Models.Entity? GetTargetEntity(int entityId) => targetEntities.GetValueOrDefault(entityId);

        public Mob? GetTargetMob(int entityId) => targetEntities.GetValueOrDefault(entityId);

        // ----- Flow 1: server sends a player tied to a session id --------------

        public Player GetOrCreateSessionPlayer(int sessionId, CharacterClass? characterClass = null)
        {
            if (sessionPlayers.TryGetValue(sessionId, out var player))
            {
                player.CharacterClass ??= characterClass;
                return player;
            }

            player = new Player
            {
                Id = sessionId,
                Name = $"Player_{sessionId}",
                CharacterClass = characterClass,
            };

            sessionPlayers[sessionId] = player;
            return player;
        }

        public void SetSessionPlayerName(int sessionId, string name, string serverName = "", bool isUser = false)
        {
            if (sessionPlayers.TryGetValue(sessionId, out var existing) && existing.IsIdentified && existing.Name != name)
            {
                // Same session id, but it was already confirmed as a different
                // player - the id has been reused for someone else. Drop the
                // stale entity (and whatever it was linked to) and start fresh.
                sessionPlayers.Remove(sessionId);
            }

            var player = GetOrCreateSessionPlayer(sessionId);

            player.Name = name;
            if (!string.IsNullOrEmpty(serverName)) player.ServerName = serverName;
            player.IsIdentified = true;
            player.IsUser = isUser || name == currentUserName;

            if (isUser) SetCurrentUser(name);
            TryFallbackLinkByName(name);
        }

        public Player? GetPlayerEntity(int sessionId) => sessionPlayers.GetValueOrDefault(sessionId);

        // ----- Flow 2: server sends global player metadata, any time -----------

        public void RegisterOrUpdateGlobalPlayer(Player data)
        {
            var globalId = data.Id;

            if (!globalPlayers.TryGetValue(globalId, out var identity))
            {
                identity = new Player { Id = globalId };
                globalPlayers[globalId] = identity;
            }

            identity.Name = data.Name;
            identity.CharacterLevel = data.CharacterLevel;
            identity.CombatPower = data.CombatPower;
            identity.ServerId = data.ServerId;
            identity.ServerName = data.ServerName;
            if (data.CharacterClass != null) identity.CharacterClass = data.CharacterClass;
            identity.IsIdentified = true;
            identity.IsUser = identity.IsUser || identity.Name == currentUserName;

            PropagateIdentityToLinkedSessions(globalId);
            TryFallbackLinkByName(identity.Name);
        }

        // ----- Flow 3: packet carries both a global id and a session id --------

        public void LinkSessionToGlobalPlayer(int globalId, int sessionId)
        {
            var session = GetOrCreateSessionPlayer(sessionId);
            session.GlobalId = globalId;

            if (!globalPlayers.TryGetValue(globalId, out var identity))
            {
            
                identity = new Player
                {
                    Id = globalId,
                    Name = session.Name,
                    CharacterLevel = session.CharacterLevel,
                    CombatPower = session.CombatPower,
                    ServerId = session.ServerId,
                    ServerName = session.ServerName,
                    CharacterClass = session.CharacterClass,
                    IsUser = session.IsUser,
                    IsIdentified = session.IsIdentified,
                };
                globalPlayers[globalId] = identity;
            }

            ApplyIdentity(session, identity);
        }

        private void PropagateIdentityToLinkedSessions(int globalId)
        {
            if (!globalPlayers.TryGetValue(globalId, out var identity)) return;

            foreach (var session in sessionPlayers.Values)
            {
                if (session.GlobalId == globalId) ApplyIdentity(session, identity);
            }
        }

        private static void ApplyIdentity(Player session, Player identity)
        {   
            // Only a confirmed (server-given) name should ever overwrite a session's name.
            if (identity.IsIdentified)
            {
                session.Name = identity.Name;
                session.IsIdentified = true;
            }

            session.CharacterLevel = identity.CharacterLevel;
            session.CombatPower = identity.CombatPower;
            session.ServerId = identity.ServerId;
            if (!string.IsNullOrEmpty(identity.ServerName)) session.ServerName = identity.ServerName;
            session.CharacterClass ??= identity.CharacterClass;
            session.IsUser = session.IsUser || identity.IsUser;
            session.GlobalId = identity.Id;
        }

        // Link packets are sometimes missing entirely. As a fallback, if we
        // see the same name show up on both an unlinked session player and a
        // global identity, assume they're the same person and link them.
        private void TryFallbackLinkByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return;

            var session = sessionPlayers.Values.FirstOrDefault(p => p.GlobalId == null && p.Name == name);
            if (session == null) return;

            var identity = globalPlayers.Values.FirstOrDefault(p => p.Name == name);
            if (identity == null) return;

            session.GlobalId = identity.Id;
            ApplyIdentity(session, identity);
        }

        private void SetCurrentUser(string name)
        {
            currentUserName = name;

  
            foreach (var player in sessionPlayers.Values)
            {
                if (player.Name == name) player.IsUser = true;
            }
            foreach (var identity in globalPlayers.Values)
            {
                if (identity.Name == name) identity.IsUser = true;
            }
        }

        // ----- Summons -----------------------------------------------------------

        public void RegisterSummon(int summonId, int ownerId)
        {
            summons[summonId] = ownerId;
            SummonRegistered?.Invoke(summonId, ownerId);
        }

        public bool IsSummon(int entityId) => summons.ContainsKey(entityId);

        public int? GetSummonOwner(int summonId) => summons.TryGetValue(summonId, out var ownerId) ? ownerId : null;

        public void Clear()
        {
            // Players persist across a Clear() (e.g. a new pull in the same session);
            // only per-fight bookkeeping like summons resets.
            summons.Clear();
        }
    }
}