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

            AdvanceMovement(session);
            var geometry = _navigation.Geometry(room);
            if (!geometry.HasLineOfSight(npc.TalkPoint, npc.Position, clearance: 0))
                return new("Der Weg zur Figur ist versperrt.");
            if (session.State == "walk" || session.Position.DistanceTo(npc.TalkPoint) > 2)
                return new("Du bist noch nicht in Gesprächsreichweite.", TooFar: true);
            if (!geometry.HasLineOfSight(session.Position, npc.Position, clearance: 0))
                return new("Du kannst die Figur von hier nicht ansprechen.");

            var options = GetAvailableOptions(room, npc);
            if (request.Choice is not { } choice) return new(null, options);

            var selected = options.FirstOrDefault(option => option == choice);
            return selected is null
                ? new("Das Gespräch hat sich geändert. Wähle eine aktuelle Frage.", options)
                : new(null, options, Selected: selected);
        }
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
