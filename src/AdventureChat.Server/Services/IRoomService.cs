using AdventureChat.Server.Models;
using YamlDotNet.Serialization;

namespace AdventureChat.Server.Services;

public interface IRoomService
{
    RoomDefinition? GetRoom(string roomId);
    IReadOnlyList<RoomDefinition> GetAllRooms();
    void RegisterOrUpdateRoom(RoomDefinition room);
    void ReloadRooms();
}

public sealed class RoomService : IRoomService
{
    private static readonly RoomDefinition DefaultLounge = new(
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
            new RoomExit(
                "to-garden",
                new Point2D(580, 300),
                45.0,
                "garden",
                "from-lounge",
                new Rect2D(535, 75, 80, 260),
                "Tavernen-Garten"
            )
        ],
        Notes: "Der gemütliche Gastraum der Taverne."
    );

    private static readonly RoomDefinition DefaultGarden = new(
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
            new RoomExit(
                "to-lounge",
                new Point2D(60, 300),
                45.0,
                "lounge",
                "from-garden",
                new Rect2D(25, 95, 75, 240),
                "Lounge"
            )
        ],
        Notes: "Der lauschige Gartenbereich hinter der Taverne."
    );

    private readonly IContentPathProvider? _pathProvider;
    private readonly IRoomYamlSerializer? _yamlSerializer;
    private readonly IRoomValidator? _validator;
    private readonly Dictionary<string, RoomDefinition> _rooms = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public RoomService(IContentPathProvider? pathProvider = null, IRoomYamlSerializer? yamlSerializer = null, IRoomValidator? validator = null, WorldPackage? world = null)
    {
        _pathProvider = pathProvider;
        _yamlSerializer = yamlSerializer;
        _validator = validator;

        if (world is not null)
        {
            foreach (var dto in world.LoadRooms())
            {
                var room = dto.ToRoomDefinition();
                if (room.Id == world.StartRoom) room = room with { SpawnPoint = room.GetSpawnPosition(world.StartSpawn) };
                _rooms.Add(room.Id, room);
            }
            return;
        }

        // Seed with built-in defaults
        _rooms[DefaultLounge.Id] = DefaultLounge;
        _rooms[DefaultGarden.Id] = DefaultGarden;

        ReloadRooms();
    }

    public RoomDefinition? GetRoom(string roomId)
    {
        lock (_lock)
        {
            return _rooms.GetValueOrDefault(roomId);
        }
    }

    public IReadOnlyList<RoomDefinition> GetAllRooms()
    {
        lock (_lock)
        {
            return _rooms.Values.ToList();
        }
    }

    public void RegisterOrUpdateRoom(RoomDefinition room)
    {
        ArgumentNullException.ThrowIfNull(room);
        lock (_lock)
        {
            _rooms[room.Id] = room;
        }
    }

    public void ReloadRooms()
    {
        if (_pathProvider is null || _yamlSerializer is null)
        {
            return;
        }

        var roomsDir = _pathProvider.GetRoomsDirectory();
        if (!Directory.Exists(roomsDir))
        {
            return;
        }

        var yamlFiles = Directory.GetFiles(roomsDir, "*.yaml");
        var candidates = new List<(string File, RoomYamlDto Dto)>();
        foreach (var file in yamlFiles)
        {
            try
            {
                var yaml = File.ReadAllText(file);
                var dto = _yamlSerializer.Deserialize(yaml);
                var raw = new DeserializerBuilder().WithDuplicateKeyChecking().WithAttemptingUnquotedStringTypeDeserialization().Build().Deserialize<object>(yaml);
                var json = new SerializerBuilder().JsonCompatible().Build().Serialize(raw);
                var schema = _validator?.ValidateJson(json);
                var validation = _validator?.Validate(dto);
                if (schema is { IsValid: false } || validation is { IsValid: false })
                    throw new InvalidOperationException(string.Join("; ", (schema?.Errors ?? []).Concat(validation?.Errors ?? [])));
                candidates.Add((file, dto));
            }
            catch (Exception ex) { throw new InvalidOperationException($"Invalid room file '{file}': {ex.Message}", ex); }
        }
        if (candidates.Count > 0 && _validator is not null)
        {
            var world = _validator.ValidateWorld(candidates.Select(c => c.Dto).ToList());
            if (!world.IsValid) throw new InvalidOperationException($"Invalid world ({string.Join(", ", candidates.Select(c => c.File))}): {string.Join("; ", world.Errors)}");
        }
        lock (_lock)
        {
            foreach (var candidate in candidates)
                _rooms[candidate.Dto.Id] = candidate.Dto.ToRoomDefinition();
        }
    }
}

public sealed class RoomStartupValidation(IRoomService rooms) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = rooms.GetAllRooms();
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
