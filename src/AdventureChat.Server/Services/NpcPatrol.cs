using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

internal sealed class NpcPatrol(NpcDefinition definition, double now)
{
    private const double DwellMs = 1000;
    private const double PositionTolerance = 0.0001;
    private readonly List<Point2D> _route = [];
    private readonly IReadOnlyList<Point2D> _waypoints = definition.Waypoints ?? [];
    private double _updatedAt = now;
    private double _waitMs;
    private bool _blocked;
    private bool _paused;
    private int _nextWaypoint;
    private string _direction = "down";
    private Point2D? _reactionTarget;

    public string Id => definition.Id;
    public Point2D Position { get; private set; } = definition.Position;

    public void Advance(double now, RoomGeometry geometry, double speed, IPathfindingService pathfinder, bool paused)
    {
        var elapsed = Math.Max(0, now - _updatedAt);
        _updatedAt = now;
        if (paused || _paused) elapsed = 0;
        _paused = paused;
        if (paused) return;

        if (_reactionTarget is not null)
        {
            if (!FollowRoute(ref elapsed, speed)) return;
            _reactionTarget = null;
        }
        if (_waypoints.Count == 0 || _blocked) return;

        while (elapsed >= _waitMs)
        {
            elapsed -= _waitMs;
            _waitMs = 0;
            if (_route.Count == 0 && !PlanRoute(geometry, pathfinder, _waypoints[_nextWaypoint]))
            {
                _blocked = true;
                return;
            }
            if (!FollowRoute(ref elapsed, speed)) return;
            _nextWaypoint = (_nextWaypoint + 1) % _waypoints.Count;
            _waitMs = DwellMs;
        }
        _waitMs -= elapsed;
    }

    public void React(Point2D target, RoomGeometry geometry, IPathfindingService pathfinder)
    {
        if (_reactionTarget is not null || Position.DistanceTo(target) < PositionTolerance ||
            !PlanRoute(geometry, pathfinder, target)) return;
        _reactionTarget = target;
        _blocked = false;
        _direction = Facing(Position, _route[0]);
    }

    private bool PlanRoute(RoomGeometry geometry, IPathfindingService pathfinder, Point2D target)
    {
        var path = pathfinder.CalculatePath(geometry, Position, target);
        if (path.Waypoints is not { } route) return false;
        _route.Clear();
        _route.AddRange(route.Skip(1));
        return true;
    }

    private bool FollowRoute(ref double elapsed, double speed)
    {
        while (_route.Count > 0)
        {
            var next = _route[0];
            var duration = Position.DistanceTo(next) / speed * 1000;
            _direction = Facing(Position, next);
            if (elapsed < duration)
            {
                var t = elapsed / duration;
                Position = new(Position.X + (next.X - Position.X) * t, Position.Y + (next.Y - Position.Y) * t);
                return false;
            }
            elapsed -= duration;
            Position = next;
            _route.RemoveAt(0);
        }
        return true;
    }

    public void NavigationChanged(RoomGeometry geometry, IPathfindingService pathfinder)
    {
        _blocked = false;
        if (_route.Prepend(Position).Zip(_route).All(leg => geometry.HasLineOfSight(leg.First, leg.Second))) return;
        _route.Clear();
        if (_reactionTarget is { } target && (!PlanRoute(geometry, pathfinder, target) || _route.Count == 0))
            _reactionTarget = null;
    }

    public NpcPresence Presence(RoomGeometry geometry)
    {
        var offset = new Point2D(Position.X + definition.TalkPoint.X - definition.Position.X,
            Position.Y + definition.TalkPoint.Y - definition.Position.Y);
        var talkPoint = geometry.HasLineOfSight(Position, offset, clearance: 0) ? offset : Position;
        return new(Id, definition.Name, definition.CharacterId, Position, talkPoint,
            new(_direction, _paused || _route.Count == 0 ? [] : [Position, .._route]));
    }

    public void HandOffToGuide()
    {
        Position = definition.Position;
        _route.Clear();
        _reactionTarget = null;
        _direction = "down";
    }

    public NpcPatrolCheckpoint Checkpoint() => new(Id, Position, _nextWaypoint, _waitMs, _blocked, _direction, [.._route],
        _reactionTarget);

    public void Restore(NpcPatrolCheckpoint checkpoint, RoomGeometry geometry)
    {
        if (!geometry.IsWalkable(checkpoint.Position) || checkpoint.NextWaypoint < 0 ||
            checkpoint.NextWaypoint >= Math.Max(1, _waypoints.Count) || !double.IsFinite(checkpoint.WaitMs) ||
            checkpoint.WaitMs is < 0 or > DwellMs || checkpoint.Direction is not ("up" or "down" or "left" or "right"))
            throw new ArgumentException("Invalid preview NPC checkpoint.");
        var destination = checkpoint.ReactionTarget ?? (_waypoints.Count > 0 ? _waypoints[checkpoint.NextWaypoint] : (Point2D?)null);
        if (checkpoint.Route is null || (checkpoint.ReactionTarget is not null && checkpoint.Route.Count == 0) ||
            (checkpoint.Route.Count > 0 &&
            (checkpoint.Blocked || (checkpoint.ReactionTarget is null && checkpoint.WaitMs > 0) || checkpoint.Route[^1] != destination ||
                !checkpoint.Route.Prepend(checkpoint.Position).Zip(checkpoint.Route)
                    .All(leg => geometry.HasLineOfSight(leg.First, leg.Second)))))
            throw new ArgumentException("Invalid preview NPC route.");
        Position = checkpoint.Position;
        _nextWaypoint = checkpoint.NextWaypoint;
        _waitMs = checkpoint.WaitMs;
        _blocked = checkpoint.Blocked;
        _direction = checkpoint.Direction;
        _reactionTarget = checkpoint.ReactionTarget;
        _route.AddRange(checkpoint.Route);
    }

    private string Facing(Point2D from, Point2D to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        if (Math.Abs(dx) < PositionTolerance && Math.Abs(dy) < PositionTolerance) return _direction;
        return Math.Abs(dx) >= Math.Abs(dy) ? (dx < 0 ? "left" : "right") : (dy < 0 ? "up" : "down");
    }
}
