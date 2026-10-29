using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

public interface IRoomService
{
    RoomDefinition? GetRoom(string roomId);
    IReadOnlyList<RoomDefinition> GetAllRooms();
}

public sealed class RoomService : IRoomService
{
    private static readonly RoomDefinition Lounge = new(
        Id: "lounge",
        Name: "Lounge",
        Width: 640,
        Height: 360,
        BackgroundUrl: "assets/images/lounge.png",
        SpawnPoint: new Point2D(120, 300),
        Geometry: new RoomGeometry(
            OuterBoundary: new Polygon([
                new Point2D(40, 240),
                new Point2D(300, 220),
                new Point2D(600, 240),
                new Point2D(600, 345),
                new Point2D(40, 345)
            ]),
            Obstacles: [
                new Polygon([
                    new Point2D(250, 255),
                    new Point2D(390, 255),
                    new Point2D(390, 310),
                    new Point2D(250, 310)
                ])
            ]
        )
    );

    private readonly Dictionary<string, RoomDefinition> _rooms = new(StringComparer.OrdinalIgnoreCase)
    {
        [Lounge.Id] = Lounge
    };

    public RoomDefinition? GetRoom(string roomId)
    {
        return _rooms.GetValueOrDefault(roomId);
    }

    public IReadOnlyList<RoomDefinition> GetAllRooms()
    {
        return _rooms.Values.ToList();
    }
}
