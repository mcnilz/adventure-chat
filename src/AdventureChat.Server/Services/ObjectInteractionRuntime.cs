using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

// The owning session manager serializes all calls with its session/movement lock.
internal sealed class ObjectInteractionRuntime(IRoomService rooms, TimeProvider clock)
{
    private sealed record Assignment(string Room, string Object, ObjectInteraction Interaction, string State);
    private sealed record ActiveTransition(string From, long StartedAtMs, int DurationMs)
    {
        public long EndsAtMs => StartedAtMs + DurationMs;
    }

    private readonly Dictionary<(string Room, string Object), string> _states = new();
    private readonly Dictionary<(string Room, string Object), ActiveTransition> _transitions = new();
    private readonly Dictionary<string, long> _revisions = new(StringComparer.OrdinalIgnoreCase);
    private long _revision;

    public long Revision(string roomId) => _revisions.GetValueOrDefault(roomId);
    public void SetRevision(string roomId, long revision) => _revisions[roomId] = revision;
    public void AdvanceRevision(string roomId) => SetRevision(roomId, ++_revision);

    public string State(string roomId, RoomObject obj) => _states.GetValueOrDefault((roomId, obj.Id), obj.Interaction!.InitialState);

    public ObjectAction? FindAction(string roomId, RoomObject obj, string actionId)
    {
        var current = State(roomId, obj);
        return obj.Interaction!.Actions.Find(action => action.Id == actionId && action.From == current);
    }

    public static IReadOnlyList<Point2D> UsePoints(RoomObject obj) =>
        obj.UsePoints ?? (obj.Interaction?.UsePoint is { } old ? [new Point2D(old.X, old.Y)] : []);
    public static Point2D AnimationPoint(RoomObject obj, ObjectActorAnimation animation) => UsePoints(obj)[animation.Point];
    public static bool AtAnimationPoint(Point2D actor, Point2D point) => actor.DistanceTo(point) <= 1;

    public void RestorePreview(IReadOnlyList<PreviewObjectStates> restored)
    {
        foreach (var group in restored)
        {
            var room = rooms.GetRoom(group.RoomId) ?? throw new ArgumentException("Unknown preview room.");
            foreach (var state in group.States)
            {
                var interaction = room.Objects?.SingleOrDefault(o => o.Id == state.ObjectId)?.Interaction;
                if (interaction?.States.Any(s => s.Id == state.StateId) != true)
                    throw new ArgumentException("Unknown preview object or state.");
                if (!_states.TryAdd((room.Id, state.ObjectId), state.StateId))
                    throw new ArgumentException("Duplicate preview object state.");
                if (state.Transition is not { } transition) continue;
                var duration = Duration(interaction, transition.From, state.StateId);
                var now = Now();
                if (duration is null || transition.StartedAtMs > now || transition.StartedAtMs < 0)
                    throw new ArgumentException("Invalid preview transition.");
                if (transition.StartedAtMs + duration.Value > now)
                    _transitions.Add((room.Id, state.ObjectId), new(transition.From, transition.StartedAtMs, duration.Value));
            }
        }
    }

    public IReadOnlyList<ObjectStateDto> States(RoomDefinition room)
    {
        var now = Now();
        return (room.Objects ?? []).Where(o => o.Interaction is not null).Select(o =>
        {
            var key = (room.Id, o.Id);
            var state = _states.GetValueOrDefault(key, o.Interaction!.InitialState);
            var moving = Active(key, now);
            return new ObjectStateDto(o.Id, state, moving is null ? null :
                new(moving.From, moving.StartedAtMs, moving.DurationMs, (int)Math.Max(0, now - moving.StartedAtMs)));
        }).ToList();
    }

    public void Reset()
    {
        _states.Clear();
        _transitions.Clear();
        var revision = ++_revision;
        foreach (var room in rooms.GetAllRooms()) _revisions[room.Id] = revision;
    }

    public ObjectActionResult Execute(PlayerSession session, ObjectActionRequestDto request,
        Func<ObjectNavigation, IReadOnlyList<string>, string?>? validate = null)
    {
        ObjectActionResult Reject(string message, string? code = null) => new(false, message, [], code);
        if (!string.Equals(request.RoomId, session.RoomId, StringComparison.OrdinalIgnoreCase))
            return Reject("Der Raum hat sich geändert. Bitte erneut versuchen.");
        var room = rooms.GetRoom(session.RoomId);
        var obj = room?.Objects?.FirstOrDefault(o => o.Id == request.ObjectId);
        if (room is null || obj?.Interaction is not { } interaction) return Reject("Objekt nicht verfügbar.");
        if (request.ExpectedRevision != Revision(room.Id)) return Reject("Der Objektzustand hat sich geändert. Bitte erneut versuchen.");
        var action = FindAction(room.Id, obj, request.ActionId);
        if (action?.ActorAnimation is { } animation)
        {
            var point = AnimationPoint(obj, animation);
            if (!new ObjectNavigation(rooms, States).Geometry(room).IsWalkable(point))
                return Reject("Der Bedienpunkt ist derzeit gesperrt.");
            if (!AtAnimationPoint(session.Position, point)) return Reject("Zu weit entfernt. Bitte zum Bedienpunkt gehen.", "too_far");
        }
        else if (!UsePoints(obj).Any(point => session.Position.DistanceTo(point) <= interaction.UseRadius))
            return Reject("Zu weit entfernt. Bitte näher herangehen.", "too_far");
        if (action is null) return Reject("Diese Aktion ist derzeit nicht möglich.");
        var assignments = new List<Assignment> { new(room.Id, obj.Id, interaction, action.To) };
        foreach (var effect in action.Effects ?? [])
        {
            var targetRoom = rooms.GetRoom(effect.Room);
            var target = targetRoom?.Objects?.FirstOrDefault(o => o.Id == effect.Object)?.Interaction;
            if (targetRoom is null || target is null || !target.States.Any(s => s.Id == effect.State))
                return Reject("Das Aktionsziel ist nicht mehr verfügbar.");
            assignments.Add(new(targetRoom.Id, effect.Object, target, effect.State));
        }
        var now = Now();
        foreach (var assignment in assignments)
        {
            var key = (assignment.Room, assignment.Object);
            if (_states.GetValueOrDefault(key, assignment.Interaction.InitialState) != assignment.State && Active(key, now) is not null)
                return Reject("Das Objekt bewegt sich noch. Bitte warte, bis die Bewegung beendet ist.");
        }
        var affectedIds = assignments.Select(a => a.Room).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var proposedStates = assignments.ToDictionary(a => (a.Room, a.Object), a => a.State);
        var proposed = new ObjectNavigation(rooms, r => States(r)
            .Select(s => new ObjectStateDto(s.ObjectId, proposedStates.GetValueOrDefault((r.Id, s.ObjectId), s.StateId))).ToList());
        if (validate?.Invoke(proposed, affectedIds) is { } error) return Reject(error);
        var startedAt = Now();
        foreach (var assignment in assignments)
        {
            var key = (assignment.Room, assignment.Object);
            var previous = _states.GetValueOrDefault(key, assignment.Interaction.InitialState);
            if (previous == assignment.State) continue;
            _states[key] = assignment.State;
            if (Duration(assignment.Interaction, previous, assignment.State) is int duration)
                _transitions[key] = new(previous, startedAt, duration);
        }
        var revision = ++_revision;
        var changes = new List<ObjectStatesChangedDto>();
        foreach (var roomId in affectedIds)
        {
            _revisions[roomId] = revision;
            var affected = rooms.GetRoom(roomId)!;
            changes.Add(new ObjectStatesChangedDto(affected.Id, revision, States(affected)));
        }
        return new ObjectActionResult(true, null, changes);
    }

    private long Now() => clock.GetUtcNow().ToUnixTimeMilliseconds();

    private ActiveTransition? Active((string Room, string Object) key, long now)
    {
        if (!_transitions.TryGetValue(key, out var transition)) return null;
        if (transition.EndsAtMs > now) return transition;
        _transitions.Remove(key);
        return null;
    }

    private static int? Duration(ObjectInteraction interaction, string from, string to) =>
        interaction.Transitions?.Find(transition => transition.From == from && transition.To == to)?.DurationMs;
}
