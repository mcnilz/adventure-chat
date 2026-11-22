using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

public partial class RoomSessionManager
{
    private const double NpcCorrectionIntervalMs = 1000;
    private const double NpcApproachGraceMs = 5000;
    private readonly Dictionary<string, NpcPatrol[]> _npcPatrols = new();
    private readonly Dictionary<string, NpcDialogueApproach> _npcApproaches = new();
    private readonly Dictionary<string, NpcBroadcast> _npcBroadcasts = new();
    private long _npcSample;
    private double _nextEmptyRoomUpdate;

    private sealed record NpcDialogueApproach(string Id, string RoomId, string NpcId, Point2D Point, double ExpiresAt);
    private sealed record NpcBroadcast(double Timestamp, IReadOnlyList<NpcPresence> Npcs);

    private double NpcNow => _clock.GetUtcNow().ToUnixTimeMilliseconds();

    private static IEnumerable<ObjectNpcReaction> NpcReactions(RoomDefinition room) =>
        (room.Objects ?? []).SelectMany(obj => obj.Interaction?.Actions ?? [])
            .Select(action => action.NpcReaction).OfType<ObjectNpcReaction>();

    private static IEnumerable<NpcDefinition> MovingNpcs(RoomDefinition room)
    {
        if (room.Npcs is not { Count: > 0 }) return [];
        var calledNpcs = NpcReactions(room).Select(reaction => reaction.Npc).ToHashSet(StringComparer.Ordinal);
        return room.Npcs.Where(npc => npc.Waypoints is not null || calledNpcs.Contains(npc.Id));
    }

    private bool NpcHasConversation(string roomId, string npcId, double now) =>
        _npcApproaches.Values.Any(approach => approach.RoomId == roomId && approach.NpcId == npcId &&
            approach.ExpiresAt > now);

    private void StartNpcReaction(string roomId, ObjectNpcReaction reaction, double now)
    {
        if (_roomService.GetRoom(roomId) is not { } room ||
            GuideIsBusy(room.Id, reaction.Npc) || NpcHasConversation(room.Id, reaction.Npc, now)) return;
        var npc = UpdateNpcPatrols(room, now).FirstOrDefault(patrol => patrol.Id == reaction.Npc);
        npc?.React(reaction.Position, _navigation.Geometry(room), _pathfinder);
    }

    private NpcPatrol[] UpdateNpcPatrols(RoomDefinition room, double now)
    {
        if (!_npcPatrols.TryGetValue(room.Id, out var patrols))
        {
            patrols = MovingNpcs(room).Select(npc => new NpcPatrol(npc, now)).ToArray();
            if (patrols.Length == 0) return [];
            _npcPatrols.Add(room.Id, patrols);
        }
        var geometry = _navigation.Geometry(room);
        foreach (var patrol in patrols)
        {
            if (!GuideIsBusy(room.Id, patrol.Id))
                patrol.Advance(now, geometry, room.WalkSpeed, _pathfinder, NpcHasConversation(room.Id, patrol.Id, now));
        }
        return patrols;
    }

    internal IReadOnlyList<NpcMovementDto> CollectNpcMovement()
    {
        lock (_lock)
        {
            var now = NpcNow;
            foreach (var expired in _npcApproaches.Where(pair => pair.Value.ExpiresAt <= now).Select(pair => pair.Key).ToArray())
                _npcApproaches.Remove(expired);
            var occupied = _sessionsByConnection.Values.Select(player => player.RoomId).ToHashSet();
            var updates = new List<NpcMovementDto>();
            var updateEmptyRooms = now >= _nextEmptyRoomUpdate;
            if (updateEmptyRooms) _nextEmptyRoomUpdate = now + NpcCorrectionIntervalMs;
            var guideRooms = TourGuides().Select(guide => guide.Actor.RoomId);
            var rooms = _npcPatrols.Keys.Concat(guideRooms).Concat(_tourNpcRooms).Distinct().ToArray();
            _tourNpcRooms.Clear();
            foreach (var roomId in rooms)
            {
                var hasVisitors = occupied.Contains(roomId);
                if (!hasVisitors && !updateEmptyRooms) continue;
                if (_roomService.GetRoom(roomId) is not { } room) continue;
                if (!hasVisitors)
                {
                    UpdateNpcPatrols(room, now);
                    _npcBroadcasts.Remove(roomId);
                    continue;
                }
                var npcs = NpcPresences(room, now) ?? [];
                if (_npcBroadcasts.TryGetValue(roomId, out var previous) && SameNpcRoutes(previous.Npcs, npcs) &&
                    (now - previous.Timestamp < NpcCorrectionIntervalMs || !npcs.Any(npc => npc.Motion is { Route.Count: > 1 })))
                    continue;
                _npcBroadcasts[roomId] = new(now, npcs);
                updates.Add(new(roomId, ++_npcSample, npcs));
            }
            return updates;
        }
    }

    private static bool SameNpcRoutes(IReadOnlyList<NpcPresence> previous, IReadOnlyList<NpcPresence> current) =>
        previous.Count == current.Count && previous.Zip(current).All(pair =>
        {
            if (pair.First.Id != pair.Second.Id) return false;
            var before = pair.First.Motion;
            var after = pair.Second.Motion;
            if (before is null || after is null) return before == after;
            return before.Direction == after.Direction && before.Route.Count == after.Route.Count &&
                (after.Route.Count == 0 ? pair.First.Position == pair.Second.Position
                    : before.Route.Skip(1).SequenceEqual(after.Route.Skip(1)));
        });

    private IReadOnlyList<NpcPresence>? NpcPresences(RoomDefinition room, double now)
    {
        var guides = TourGuides().Where(guide => guide.Actor.RoomId == room.Id).ToArray();
        if (room.Npcs is null && guides.Length == 0) return null;
        var patrols = UpdateNpcPatrols(room, now);
        var geometry = _navigation.Geometry(room);
        var npcs = (room.Npcs ?? []).Where(npc => !GuideIsBusy(room.Id, npc.Id))
            .Select(npc => patrols.FirstOrDefault(patrol => patrol.Id == npc.Id)?.Presence(geometry) ??
                new NpcPresence(npc.Id, npc.Name, npc.CharacterId, npc.Position, npc.TalkPoint)).ToList();
        npcs.AddRange(guides.Select(guide => guide.Presence()));
        return npcs;
    }

    private RoomDefinitionDto SnapshotRoom(RoomDefinition room)
    {
        var npcs = NpcPresences(room, NpcNow);
        return _navigation.EffectiveRoom(room).ToDto() with { Npcs = npcs, NpcSample = ++_npcSample };
    }

    private void CancelNpcApproach(string connectionId, string? approachId = null, Point2D? movementTarget = null)
    {
        if (_npcApproaches.TryGetValue(connectionId, out var approach) &&
            (approachId is null || approach.Id == approachId) &&
            (movementTarget is null || approach.Point.DistanceTo(movementTarget.Value) > 0.01 ||
                double.IsPositiveInfinity(approach.ExpiresAt)))
            _npcApproaches.Remove(connectionId);
    }

    private void ExtendNpcApproach(PlayerSession player, RoomDefinition room)
    {
        if (_npcApproaches.TryGetValue(player.ConnectionId, out var approach) && double.IsFinite(approach.ExpiresAt))
            _npcApproaches[player.ConnectionId] = approach with
            {
                ExpiresAt = ArrivalAt(player, room).ToUnixTimeMilliseconds() + NpcApproachGraceMs
            };
    }

    public void EndNpcDialogue(string connectionId, string approachId)
    {
        lock (_lock)
            CancelNpcApproach(connectionId, approachId);
    }

    internal void RestorePreviewPatrols(RoomDefinition room, PreviewNpcPatrols? preview)
    {
        if (preview is null) return;
        var now = NpcNow;
        var start = now - Math.Clamp(preview.ElapsedMs, 0, 300_000);
        var resumeAt = preview.ResumeAfterMs is { } delay ? Math.Min(now, start + Math.Max(0, delay)) : (double?)null;
        var patrols = MovingNpcs(room)
            .Select(npc => new NpcPatrol(npc, npc.Id == preview.PausedNpc ? resumeAt ?? start : start)).ToArray();
        if (preview.Checkpoints is null || preview.Checkpoints.Count != patrols.Length ||
            preview.Checkpoints.Any(checkpoint => checkpoint is null) ||
            preview.Checkpoints.Select(checkpoint => checkpoint.Id).Distinct().Count() != patrols.Length)
            throw new ArgumentException("Invalid preview NPC checkpoints.");
        if (patrols.Length == 0) return;
        var geometry = _navigation.Geometry(room);
        foreach (var checkpoint in preview.Checkpoints)
        {
            var patrol = patrols.FirstOrDefault(patrol => patrol.Id == checkpoint.Id)
                ?? throw new ArgumentException("Unknown preview NPC.");
            if (checkpoint.ReactionTarget is { } target &&
                !NpcReactions(room).Any(reaction => reaction.Npc == checkpoint.Id && reaction.Position == target))
                throw new ArgumentException("Unknown preview NPC reaction target.");
            patrol.Restore(checkpoint, geometry);
            var paused = preview.PausedNpc == patrol.Id && resumeAt is null;
            patrol.Advance(now, geometry, room.WalkSpeed, _pathfinder, paused);
            if (paused)
                _npcApproaches["preview"] = new("preview", room.Id, patrol.Id, patrol.Presence(geometry).TalkPoint,
                    double.PositiveInfinity);
        }
        _npcPatrols[room.Id] = patrols;
    }

    internal PreviewNpcPatrols? PreviewPatrols(string roomId)
    {
        if (!_npcPatrols.TryGetValue(roomId, out var patrols)) return null;
        return new(patrols.Select(patrol => patrol.Checkpoint()).ToList(),
            PausedNpc: _npcApproaches.GetValueOrDefault("preview")?.NpcId);
    }
}
