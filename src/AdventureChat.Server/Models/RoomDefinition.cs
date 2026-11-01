namespace AdventureChat.Server.Models;

public sealed record RoomSpawn(
    string Id,
    Point2D Position
);

public sealed record RoomExit(
    string Id,
    Point2D Position,
    double Radius,
    string TargetRoomId,
    string TargetSpawnId
);

public sealed record RoomDefinition(
    string Id,
    string Name,
    int Width,
    int Height,
    string BackgroundUrl,
    Point2D SpawnPoint,
    RoomGeometry Geometry,
    IReadOnlyList<RoomSpawn>? Spawns = null,
    IReadOnlyList<RoomExit>? Exits = null
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
