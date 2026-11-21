using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

public partial class RoomSessionManager
{
    public NpcDialogueResult RequestNpcDialogue(string connectionId, NpcDialogueRequest request)
    {
        lock (_lock)
        {
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

            return new(null, npc.Options);
        }
    }
}
