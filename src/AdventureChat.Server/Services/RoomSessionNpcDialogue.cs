using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

public partial class RoomSessionManager
{
    public NpcDialogueResult RequestNpcDialogue(string connectionId, NpcDialogueRequest request)
    {
        lock (_lock)
        {
            ProcessActorActivities();
            if (!_sessionsByConnection.TryGetValue(connectionId, out var session) || session.RoomId != request.RoomId)
                return new("Das Gespräch ist hier nicht mehr verfügbar.");

            var room = _roomService.GetRoom(session.RoomId);
            var npc = room?.Npcs?.FirstOrDefault(candidate => candidate.Id == request.NpcId);
            if (room is null || npc is null) return new("Diese Figur ist nicht im Raum.");

            var now = NpcNow;
            AdvanceMovement(session);
            var geometry = _navigation.Geometry(room);
            var patrol = UpdateNpcPatrols(room, now).FirstOrDefault(candidate => candidate.Id == npc.Id);
            var presence = patrol?.Presence(geometry);
            var position = presence?.Position ?? npc.Position;
            var talkPoint = presence?.TalkPoint ?? npc.TalkPoint;
            if (patrol is not null)
            {
                if (request.ApproachId is not { Length: > 0 and <= 64 } approachId)
                    return new("Sprich die Figur erneut an.");
                if (request.Prepare)
                    return PrepareNpcDialogue(session, room, npc.Id, talkPoint, approachId, now, geometry);
                if (!_npcApproaches.TryGetValue(connectionId, out var approach) || approach.Id != approachId ||
                    approach.NpcId != npc.Id || approach.RoomId != room.Id || approach.ExpiresAt <= now)
                    return new("Sprich die Figur erneut an.");
                talkPoint = approach.Point;
            }
            if (!geometry.HasLineOfSight(talkPoint, position, clearance: 0))
                return new("Der Weg zur Figur ist versperrt.");
            if (session.State == "walk" || session.Position.DistanceTo(talkPoint) > 2)
                return new("Du bist noch nicht in Gesprächsreichweite.", TooFar: true);
            if (!geometry.HasLineOfSight(session.Position, position, clearance: 0))
                return new("Du kannst die Figur von hier nicht ansprechen.");

            if (patrol is not null)
                _npcApproaches[connectionId] = _npcApproaches[connectionId] with { ExpiresAt = double.PositiveInfinity };
            var options = GetAvailableOptions(room, npc);
            if (request.Choice is not { } choice) return new(null, options);

            var selected = options.FirstOrDefault(option => option == choice);
            return selected is null
                ? new("Das Gespräch hat sich geändert. Wähle eine aktuelle Frage.", options)
                : new(null, options, Selected: selected);
        }
    }

    private NpcDialogueResult PrepareNpcDialogue(PlayerSession session, RoomDefinition room, string npcId,
        Point2D talkPoint, string approachId, double now, RoomGeometry geometry)
    {
        var path = _pathfinder.CalculatePath(geometry, session.Position, talkPoint);
        if (!path.Success) return new("Der Weg zur Figur ist nicht erreichbar.");
        var points = path.Waypoints!;
        var duration = points.Zip(points.Skip(1)).Sum(leg => leg.First.DistanceTo(leg.Second)) / room.WalkSpeed * 1000;
        _npcApproaches[session.ConnectionId] = new(approachId, room.Id, npcId, talkPoint, now + duration + NpcApproachGraceMs);
        UpdateNpcPatrols(room, now);
        return new(null, TalkPoint: talkPoint);
    }

    private List<AvailableDialogueOption> GetAvailableOptions(RoomDefinition room, NpcDefinition npc)
    {
        bool Matches(DialogueCondition condition) =>
            room.Objects?.FirstOrDefault(obj => obj.Id == condition.Object) is { Interaction: not null } obj &&
            _interactions.State(room.Id, obj) == condition.State;

        return npc.Options
            .Where(option => option.When is null || Matches(option.When))
            .Select(option => new AvailableDialogueOption(option.Id, option.Text,
                option.Replies?.FirstOrDefault(reply => Matches(reply.When))?.Reply ?? option.Reply))
            .ToList();
    }
}
