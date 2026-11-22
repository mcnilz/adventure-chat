using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

internal sealed class NpcPatrol(NpcDefinition definition, double now)
{
    private const double DwellMs = 1000;
    private const double PositionTolerance = 0.0001;
    private readonly List<Point2D> _route = [];
    private readonly IReadOnlyList<Point2D> _waypoints = definition.Waypoints!;
    private double _updatedAt = now;
    private double _waitMs;
    private bool _blocked;
    private bool _paused;
    private int _nextWaypoint;
    private string _direction = "down";

    public string Id => definition.Id;
    public Point2D Position { get; private set; } = definition.Position;

    public void Advance(double now, RoomGeometry geometry, double speed, IPathfindingService pathfinder, bool paused)
    {
        var elapsed = Math.Max(0, now - _updatedAt);
        _updatedAt = now;
        if (paused || _paused) elapsed = 0;
        _paused = paused;
        if (paused || _blocked) return;

        while (elapsed >= _waitMs)
        {
            elapsed -= _waitMs;
            _waitMs = 0;
            if (_route.Count == 0)
            {
                var path = pathfinder.CalculatePath(geometry, Position, _waypoints[_nextWaypoint]);
                if (!path.Success)
                {
                    _blocked = true;
                    return;
                }
                _route.AddRange(path.Waypoints!.Skip(1));
            }

            while (_route.Count > 0)
            {
                var next = _route[0];
                var duration = Position.DistanceTo(next) / speed * 1000;
                _direction = Facing(Position, next);
                if (elapsed < duration)
                {
                    var t = elapsed / duration;
                    Position = new(Position.X + (next.X - Position.X) * t, Position.Y + (next.Y - Position.Y) * t);
                    return;
                }
                elapsed -= duration;
                Position = next;
                _route.RemoveAt(0);
            }
            _nextWaypoint = (_nextWaypoint + 1) % _waypoints.Count;
            _waitMs = DwellMs;
        }
        _waitMs -= elapsed;
    }

    public void NavigationChanged(RoomGeometry geometry)
    {
        _blocked = false;
        if (!_route.Prepend(Position).Zip(_route).All(leg => geometry.HasLineOfSight(leg.First, leg.Second)))
            _route.Clear();
    }

    public NpcPresence Presence(RoomGeometry geometry)
    {
        var offset = new Point2D(Position.X + definition.TalkPoint.X - definition.Position.X,
            Position.Y + definition.TalkPoint.Y - definition.Position.Y);
        var talkPoint = geometry.HasLineOfSight(Position, offset, clearance: 0) ? offset : Position;
        return new(Id, definition.Name, definition.CharacterId, Position, talkPoint,
            new(_direction, _paused || _route.Count == 0 ? [] : [Position, .._route]));
    }

    public NpcPatrolCheckpoint Checkpoint() => new(Id, Position, _nextWaypoint, _waitMs, _blocked, _direction, [.._route]);

    public void Restore(NpcPatrolCheckpoint checkpoint, RoomGeometry geometry)
    {
        if (!geometry.IsWalkable(checkpoint.Position) || checkpoint.NextWaypoint < 0 ||
            checkpoint.NextWaypoint >= _waypoints.Count || !double.IsFinite(checkpoint.WaitMs) ||
            checkpoint.WaitMs is < 0 or > DwellMs || checkpoint.Direction is not ("up" or "down" or "left" or "right"))
            throw new ArgumentException("Invalid preview NPC checkpoint.");
        if (checkpoint.Route is null || (checkpoint.Route.Count > 0 &&
            (checkpoint.Blocked || checkpoint.WaitMs > 0 || checkpoint.Route[^1] != _waypoints[checkpoint.NextWaypoint] ||
                !checkpoint.Route.Prepend(checkpoint.Position).Zip(checkpoint.Route)
                    .All(leg => geometry.HasLineOfSight(leg.First, leg.Second)))))
            throw new ArgumentException("Invalid preview NPC route.");
        Position = checkpoint.Position;
        _nextWaypoint = checkpoint.NextWaypoint;
        _waitMs = checkpoint.WaitMs;
        _blocked = checkpoint.Blocked;
        _direction = checkpoint.Direction;
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
