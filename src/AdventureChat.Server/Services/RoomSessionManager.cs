using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

public partial class RoomSessionManager : IRoomSessionManager
{
    private readonly IRoomService _roomService;
    private readonly IPathfindingService _pathfinder;
    private readonly ConcurrentDictionary<string, PlayerSession> _sessionsByConnection = new();
    private readonly object _lock = new();

    public RoomSessionManager(IRoomService roomService, IPathfindingService pathfinder)
    {
        _roomService = roomService;
        _pathfinder = pathfinder;
    }

    public JoinResult JoinRoom(string connectionId, string roomId, string? displayName, Point2D? initialPosition = null)
    {
        var room = _roomService.GetRoom(roomId);
        if (room is null)
        {
            return new JoinResult(false, $"Room '{roomId}' not found.", null, null);
        }

        var sanitizedName = SanitizeDisplayName(displayName, connectionId);
        var spawn = initialPosition ?? room.SpawnPoint;

        // Ensure valid spawn within room geometry
        if (!room.Geometry.IsWalkable(spawn))
        {
            spawn = room.SpawnPoint;
        }

        var sessionId = Guid.NewGuid().ToString("N");
        var session = new PlayerSession
        {
            ConnectionId = connectionId,
            SessionId = sessionId,
            RoomId = roomId,
            DisplayName = sanitizedName,
            Position = spawn,
            Direction = "down",
            State = "idle",
            LastActivity = DateTime.UtcNow
        };

        lock (_lock)
        {
            // Remove previous session on this connection if any
            _sessionsByConnection.TryRemove(connectionId, out _);
            _sessionsByConnection[connectionId] = session;

            var players = _sessionsByConnection.Values
                .Where(s => s.RoomId == roomId)
                .Select(s => s.ToDto())
                .ToList();

            var snapshot = new RoomSnapshotDto(roomId, sessionId, players);
            return new JoinResult(true, null, session, snapshot);
        }
    }

    public PlayerSession? Leave(string connectionId)
    {
        lock (_lock)
        {
            if (_sessionsByConnection.TryRemove(connectionId, out var session))
            {
                return session;
            }
            return null;
        }
    }

    public PlayerSession? GetPlayer(string connectionId)
    {
        _sessionsByConnection.TryGetValue(connectionId, out var session);
        return session;
    }

    public IReadOnlyList<PlayerSession> GetPlayersInRoom(string roomId)
    {
        return _sessionsByConnection.Values
            .Where(s => s.RoomId == roomId)
            .ToList();
    }

    public MoveResult RequestMove(string connectionId, Point2D target, Point2D? currentPosition = null)
    {
        if (!_sessionsByConnection.TryGetValue(connectionId, out var session))
        {
            return new MoveResult(false, "Player session not found.", null, null, null);
        }

        var room = _roomService.GetRoom(session.RoomId);
        if (room is null)
        {
            return new MoveResult(false, $"Room '{session.RoomId}' not found.", null, null, null);
        }

        if (!target.IsFinite())
        {
            return new MoveResult(false, "Target coordinates must be finite numbers.", null, null, null);
        }

        var start = session.Position;
        if (currentPosition.HasValue && currentPosition.Value.IsFinite())
        {
            if (room.Geometry.IsWalkable(currentPosition.Value))
            {
                start = currentPosition.Value;
            }
        }

        var pathResult = _pathfinder.CalculatePath(room.Geometry, start, target);
        if (!pathResult.Success)
        {
            return new MoveResult(false, pathResult.Error ?? "Path calculation failed.", null, null, null);
        }

        var waypoints = pathResult.Waypoints!;
        lock (_lock)
        {
            session.Position = start;
            session.Target = target;
            session.Waypoints = waypoints.ToList();
            session.State = "walk";
            session.LastActivity = DateTime.UtcNow;

            if (waypoints.Count > 1)
            {
                var next = waypoints[1];
                var dx = next.X - session.Position.X;
                var dy = next.Y - session.Position.Y;
                if (Math.Abs(dx) > Math.Abs(dy))
                {
                    session.Direction = dx < 0 ? "left" : "right";
                }
                else
                {
                    session.Direction = dy < 0 ? "up" : "down";
                }
            }
        }

        var waypointDtos = waypoints.Select(w => new PointDto(w.X, w.Y)).ToList();
        return new MoveResult(true, null, session.SessionId, new PointDto(target.X, target.Y), waypointDtos);
    }

    public void UpdatePlayerProgress(string connectionId, Point2D position, string direction, string state)
    {
        if (_sessionsByConnection.TryGetValue(connectionId, out var session))
        {
            lock (_lock)
            {
                session.Position = position;
                session.Direction = direction;
                session.State = state;
                session.LastActivity = DateTime.UtcNow;
                if (state == "idle")
                {
                    session.Target = null;
                    session.Waypoints.Clear();
                }
            }
        }
    }

    private static string SanitizeDisplayName(string? name, string connectionId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            var suffix = connectionId.Length >= 4 ? connectionId[^4..] : Random.Shared.Next(1000, 9999).ToString();
            return $"Gast-{suffix}";
        }

        var trimmed = name.Trim();
        // Remove special control chars / HTML tags
        var sanitized = CleanNameRegex().Replace(trimmed, string.Empty);
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return $"Gast-{Random.Shared.Next(1000, 9999)}";
        }

        if (sanitized.Length > 24)
        {
            sanitized = sanitized[..24];
        }

        return sanitized;
    }

    [GeneratedRegex(@"[<>&""'/\\`]")]
    private static partial Regex CleanNameRegex();
}
