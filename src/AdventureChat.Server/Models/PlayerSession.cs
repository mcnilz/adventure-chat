namespace AdventureChat.Server.Models;

public class PlayerSession
{
    public string ConnectionId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Point2D Position { get; set; }
    public string Direction { get; set; } = "down";
    public string State { get; set; } = "idle";
    public List<Point2D> Waypoints { get; set; } = new();
    public Point2D? Target { get; set; }
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    public PlayerDto ToDto()
    {
        return new PlayerDto(
            SessionId,
            DisplayName,
            new PointDto(Position.X, Position.Y),
            Direction,
            State,
            Target is not null ? new PointDto(Target.Value.X, Target.Value.Y) : null,
            Waypoints.Select(w => new PointDto(w.X, w.Y)).ToList()
        );
    }
}

public record PlayerDto(
    string SessionId,
    string DisplayName,
    PointDto Position,
    string Direction,
    string State,
    PointDto? Target,
    IReadOnlyList<PointDto> Waypoints
);

public record RoomSnapshotDto(
    string RoomId,
    string YourSessionId,
    IReadOnlyList<PlayerDto> Players
);

public record JoinRoomRequestDto(
    string RoomId,
    string DisplayName
);

public record JoinRoomResultDto(
    bool Success,
    string? Error,
    RoomSnapshotDto? Snapshot
);

public record MoveResultDto(
    bool Success,
    string? Error
);

public record RequestMoveDto(
    PointDto? Start,
    PointDto Target
);

public record PlayerMovedDto(
    string SessionId,
    PointDto Target,
    IReadOnlyList<PointDto> Waypoints
);

public record UserJoinedDto(
    PlayerDto Player
);

public record UserLeftDto(
    string SessionId,
    string DisplayName
);
