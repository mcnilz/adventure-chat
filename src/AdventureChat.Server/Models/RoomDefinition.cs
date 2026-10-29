namespace AdventureChat.Server.Models;

public sealed record RoomDefinition(
    string Id,
    string Name,
    int Width,
    int Height,
    string BackgroundUrl,
    Point2D SpawnPoint,
    RoomGeometry Geometry
);
