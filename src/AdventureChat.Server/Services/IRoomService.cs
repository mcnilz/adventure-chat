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
        ),
        Spawns: [
            new RoomSpawn("entry", new Point2D(120, 300)),
            new RoomSpawn("from-garden", new Point2D(540, 300))
        ],
        Exits: [
            new RoomExit("to-garden", new Point2D(580, 300), 30.0, "garden", "from-lounge")
        ]
    );

    private static readonly RoomDefinition Garden = new(
        Id: "garden",
        Name: "Tavernen-Garten",
        Width: 640,
        Height: 360,
        BackgroundUrl: "assets/images/garden.png",
        SpawnPoint: new Point2D(80, 300),
        Geometry: new RoomGeometry(
            OuterBoundary: new Polygon([
                new Point2D(40, 240),
                new Point2D(600, 240),
                new Point2D(600, 345),
                new Point2D(40, 345)
            ]),
            Obstacles: [
                new Polygon([
                    new Point2D(260, 260),
                    new Point2D(380, 260),
                    new Point2D(380, 310),
                    new Point2D(260, 310)
                ])
            ]
        ),
        Spawns: [
            new RoomSpawn("entry", new Point2D(80, 300)),
            new RoomSpawn("from-lounge", new Point2D(80, 300))
        ],
        Exits: [
            new RoomExit("to-lounge", new Point2D(60, 300), 30.0, "lounge", "from-garden")
        ]
    );

    private readonly Dictionary<string, RoomDefinition> _rooms = new(StringComparer.OrdinalIgnoreCase)
    {
        [Lounge.Id] = Lounge,
        [Garden.Id] = Garden
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
