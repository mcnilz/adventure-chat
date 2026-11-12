using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

// The owning session manager serializes all calls with its session/movement lock.
internal sealed class ObjectInteractionRuntime(IRoomService rooms)
{
    private readonly Dictionary<(string Room, string Object), string> _states = new();
    private readonly Dictionary<string, long> _revisions = new(StringComparer.OrdinalIgnoreCase);
    private long _revision;

    public long Revision(string roomId) => _revisions.GetValueOrDefault(roomId);

    public IReadOnlyList<ObjectStateDto> States(RoomDefinition room) => (room.Objects ?? [])
        .Where(o => o.Interaction is not null)
        .Select(o => new ObjectStateDto(o.Id, _states.GetValueOrDefault((room.Id, o.Id), o.Interaction!.InitialState)))
        .ToList();

    public void Reset()
    {
        _states.Clear();
        var revision = ++_revision;
        foreach (var room in rooms.GetAllRooms()) _revisions[room.Id] = revision;
    }

    public ObjectActionResult Execute(PlayerSession session, ObjectActionRequestDto request)
    {
        ObjectActionResult Reject(string message, string? code = null) => new(false, message, [], code);
        if (!string.Equals(request.RoomId, session.RoomId, StringComparison.OrdinalIgnoreCase))
            return Reject("Der Raum hat sich geändert. Bitte erneut versuchen.");
        var room = rooms.GetRoom(session.RoomId);
        var obj = room?.Objects?.FirstOrDefault(o => o.Id == request.ObjectId);
        if (room is null || obj?.Interaction is not { } interaction) return Reject("Objekt nicht verfügbar.");
        if (request.ExpectedRevision != Revision(room.Id)) return Reject("Der Objektzustand hat sich geändert. Bitte erneut versuchen.");
        var dx = session.Position.X - interaction.UsePoint.X;
        var dy = session.Position.Y - interaction.UsePoint.Y;
        if (dx * dx + dy * dy > interaction.UseRadius * interaction.UseRadius) return Reject("Zu weit entfernt. Bitte näher herangehen.", "too_far");
        var current = _states.GetValueOrDefault((room.Id, obj.Id), interaction.InitialState);
        var action = interaction.Actions.FirstOrDefault(a => a.Id == request.ActionId && a.From == current);
        if (action is null) return Reject("Diese Aktion ist derzeit nicht möglich.");
        var assignments = new List<(string Room, string Object, string State)> { (room.Id, obj.Id, action.To) };
        foreach (var effect in action.Effects ?? [])
        {
            var targetRoom = rooms.GetRoom(effect.Room);
            var target = targetRoom?.Objects?.FirstOrDefault(o => o.Id == effect.Object)?.Interaction;
            if (targetRoom is null || target is null || !target.States.Any(s => s.Id == effect.State))
                return Reject("Das Aktionsziel ist nicht mehr verfügbar.");
            assignments.Add((targetRoom.Id, effect.Object, effect.State));
        }
        foreach (var assignment in assignments) _states[(assignment.Room, assignment.Object)] = assignment.State;
        var revision = ++_revision;
        var changes = new List<ObjectStatesChangedDto>();
        foreach (var roomId in assignments.Select(a => a.Room).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            _revisions[roomId] = revision;
            var affected = rooms.GetRoom(roomId)!;
            changes.Add(new ObjectStatesChangedDto(affected.Id, revision, States(affected)));
        }
        return new ObjectActionResult(true, null, changes);
    }
}
