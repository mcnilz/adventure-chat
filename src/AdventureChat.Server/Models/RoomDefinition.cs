namespace AdventureChat.Server.Models;

public sealed record RoomSpawn(
    string Id,
    Point2D Position
);

public sealed record Rect2D(double X, double Y, double Width, double Height);

public sealed record RoomExit(
    string Id,
    Point2D Position,
    double Radius,
    string TargetRoomId,
    string TargetSpawnId,
    Rect2D? Bounds = null,
    string? Label = null
);

public sealed record RoomObject(string Id, string Image, Point2D Position, RoomObjectSize Size, double DepthY);
public sealed record RoomObjectSize(int Width, int Height);

public sealed record DepthReference(double Y, double Factor)
{
    public DepthReference() : this(double.NaN, double.NaN) { }
}
public sealed record RoomDepthScale(DepthReference Back, DepthReference Front)
{
    public RoomDepthScale() : this(null!, null!) { }
}

public sealed record RoomViewport(int Width, int Height, string ScrollMode);

public sealed record RoomDefinition(
    string Id,
    string Name,
    int Width,
    int Height,
    string BackgroundUrl,
    Point2D SpawnPoint,
    RoomGeometry Geometry,
    IReadOnlyList<RoomSpawn>? Spawns = null,
    IReadOnlyList<RoomExit>? Exits = null,
    string? Notes = null,
    string Perspective = "adventure",
    double ActorHeight = 64,
    double WalkSpeed = 130,
    RoomViewport? Viewport = null,
    IReadOnlyList<RoomObject>? Objects = null,
    RoomDepthScale? DepthScale = null
)
{
    public IReadOnlyList<RoomSpawn> Spawns { get; init; } = Spawns ?? new List<RoomSpawn> { new("entry", SpawnPoint) };
    public IReadOnlyList<RoomExit> Exits { get; init; } = Exits ?? Array.Empty<RoomExit>();

    public Point2D GetSpawnPosition(string? spawnId)
    {
        if (!string.IsNullOrWhiteSpace(spawnId))
        {
            var matched = Spawns.FirstOrDefault(s => string.Equals(s.Id, spawnId, StringComparison.OrdinalIgnoreCase));
            if (matched is not null)
            {
                return matched.Position;
            }
        }
        return SpawnPoint;
    }
}
