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
    public DateTimeOffset MovementUpdatedAt { get; set; }
    public long MovementRevision { get; set; }
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
            Waypoints.Select(w => new PointDto(w.X, w.Y)).ToList(),
            MovementRevision
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
    IReadOnlyList<PointDto> Waypoints,
    long MovementRevision
);

public record RoomSpawnDto(
    string Id,
    PointDto Position
);

public record RectDto(double X, double Y, double Width, double Height);

public record RoomExitDto(
    string Id,
    PointDto Position,
    double Radius,
    string TargetRoomId,
    string TargetSpawnId,
    RectDto? Bounds = null,
    string? Label = null
);

public record RoomGeometryDto(
    IReadOnlyList<PointDto> OuterBoundary,
    IReadOnlyList<IReadOnlyList<PointDto>> Obstacles
);

public record RoomDefinitionDto(
    string Id,
    string Name,
    int Width,
    int Height,
    string BackgroundUrl,
    PointDto SpawnPoint,
    RoomGeometryDto Geometry,
    IReadOnlyList<RoomSpawnDto> Spawns,
    IReadOnlyList<RoomExitDto> Exits,
    string Perspective = "adventure",
    double ActorHeight = 64,
    double WalkSpeed = 130,
    RoomViewport? Viewport = null
);

public record RoomSnapshotDto(
    string RoomId,
    string YourSessionId,
    IReadOnlyList<PlayerDto> Players,
    IReadOnlyList<ChatMessageDto> RecentMessages,
    RoomDefinitionDto? Room = null
);

public record ChatMessageDto(
    string MessageId,
    string SessionId,
    string SenderName,
    string Text,
    DateTime Timestamp,
    string RoomId
);

public record SendChatMessageRequestDto(
    string Text
);

public record SendChatMessageResultDto(
    bool Success,
    string? Error,
    ChatMessageDto? Message
);

public record JoinRoomRequestDto(
    string RoomId,
    string DisplayName,
    string? WorldId = null
);

public record JoinRoomResultDto(
    bool Success,
    string? Error,
    RoomSnapshotDto? Snapshot
);

public record ChangeRoomRequestDto(string ExitId);

public record ChangeRoomResultDto(
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
    IReadOnlyList<PointDto> Waypoints,
    string RoomId,
    long Revision
);

public record UserJoinedDto(
    PlayerDto Player,
    string RoomId
);

public record UserLeftDto(
    string SessionId,
    string DisplayName,
    string RoomId
);

public static class RoomDtoExtensions
{
    public static RoomDefinitionDto ToDto(this RoomDefinition room)
    {
        return new RoomDefinitionDto(
            room.Id,
            room.Name,
            room.Width,
            room.Height,
            room.BackgroundUrl,
            new PointDto(room.SpawnPoint.X, room.SpawnPoint.Y),
            new RoomGeometryDto(
                room.Geometry.OuterBoundary.Vertices.Select(v => new PointDto(v.X, v.Y)).ToList(),
                room.Geometry.Obstacles.Select(o => (IReadOnlyList<PointDto>)o.Vertices.Select(v => new PointDto(v.X, v.Y)).ToList()).ToList()
            ),
            room.Spawns.Select(s => new RoomSpawnDto(s.Id, new PointDto(s.Position.X, s.Position.Y))).ToList(),
            room.Exits.Select(e => new RoomExitDto(
                e.Id,
                new PointDto(e.Position.X, e.Position.Y),
                e.Radius,
                e.TargetRoomId,
                e.TargetSpawnId,
                e.Bounds != null ? new RectDto(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height) : null,
                e.Label
            )).ToList(),
            room.Perspective, room.ActorHeight, room.WalkSpeed, room.Viewport
        );
    }
}
