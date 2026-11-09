using YamlDotNet.Serialization;

namespace AdventureChat.Server.Models;

public sealed class RoomYamlDto
{
    [YamlMember(Alias = "version")]
    public int Version { get; set; } = 1;

    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    [YamlMember(Alias = "name")]
    public string Name { get; set; } = string.Empty;

    [YamlMember(Alias = "background")]
    public string Background { get; set; } = string.Empty;

    [YamlMember(Alias = "size")]
    public RoomSizeYamlDto Size { get; set; } = new();

    [YamlMember(Alias = "spawns")]
    public List<RoomSpawnYamlDto> Spawns { get; set; } = new();

    [YamlMember(Alias = "walkable")]
    public List<RoomPolygonYamlDto> Walkable { get; set; } = new();

    [YamlMember(Alias = "obstacles")]
    public List<RoomPolygonYamlDto>? Obstacles { get; set; }

    [YamlMember(Alias = "exits")]
    public List<RoomExitYamlDto>? Exits { get; set; }

    [YamlMember(Alias = "notes")]
    public string? Notes { get; set; }

    public string? Perspective { get; set; }
    public RoomDepthScale? DepthScale { get; set; }
    public double? ActorHeight { get; set; }
    public double? WalkSpeed { get; set; }
    public RoomViewportYamlDto? Viewport { get; set; }
    public List<RoomObjectYamlDto>? Objects { get; set; }

    public RoomDefinition ToRoomDefinition()
    {
        var outerPoly = Walkable.Count > 0 && Walkable[0].Points.Count >= 3
            ? new Polygon(Walkable[0].Points.Select(p => new Point2D(p.X, p.Y)).ToList())
            : new Polygon([new Point2D(0, 0), new Point2D(Size.Width, 0), new Point2D(Size.Width, Size.Height), new Point2D(0, Size.Height)]);

        var obstacles = Obstacles?.Where(o => o.Points.Count >= 3)
            .Select(o => new Polygon(o.Points.Select(p => new Point2D(p.X, p.Y)).ToList()))
            .ToList() ?? new List<Polygon>();

        var spawns = Spawns.Select(s => new RoomSpawn(s.Id, new Point2D(s.Position.X, s.Position.Y))).ToList();
        var defaultSpawn = spawns.FirstOrDefault()?.Position ?? new Point2D(Size.Width / 2.0, Size.Height / 2.0);

        var exits = Exits?.Select(e => new RoomExit(
            Id: e.Id,
            Position: new Point2D(e.Position.X, e.Position.Y),
            Radius: e.Radius > 0 ? e.Radius : 30.0,
            TargetRoomId: e.Target.Room,
            TargetSpawnId: e.Target.Spawn,
            Bounds: e.Bounds != null ? new Rect2D(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height) : null,
            Label: e.Label
        )).ToList() ?? new List<RoomExit>();

        return new RoomDefinition(
            Id: Id,
            Name: Name,
            Width: Size.Width,
            Height: Size.Height,
            BackgroundUrl: Background,
            SpawnPoint: defaultSpawn,
            Geometry: new RoomGeometry(outerPoly, obstacles),
            Spawns: spawns,
            Exits: exits,
            Notes: Notes,
            Perspective: Perspective ?? "adventure",
            ActorHeight: ActorHeight ?? 64,
            DepthScale: DepthScale,
            WalkSpeed: WalkSpeed ?? 130,
            Objects: Objects?.Select(o => new RoomObject(o.Id, o.Image, new Point2D(o.Position.X, o.Position.Y), new RoomObjectSize(o.Size.Width, o.Size.Height), o.DepthY)).ToList(),
            Viewport: Viewport is null ? null : new RoomViewport(Viewport.Width, Viewport.Height, Viewport.ScrollMode)
        );
    }

    public static RoomYamlDto FromRoomDefinition(RoomDefinition room)
    {
        return new RoomYamlDto
        {
            Version = 1,
            Id = room.Id,
            Name = room.Name,
            Background = room.BackgroundUrl,
            Size = new RoomSizeYamlDto
            {
                Width = room.Width,
                Height = room.Height
            },
            Spawns = room.Spawns.Select(s => new RoomSpawnYamlDto
            {
                Id = s.Id,
                Position = new RoomPointYamlDto { X = s.Position.X, Y = s.Position.Y }
            }).ToList(),
            Walkable = [
                new RoomPolygonYamlDto
                {
                    Id = "floor",
                    Points = room.Geometry.OuterBoundary.Vertices.Select(v => new RoomPointYamlDto { X = v.X, Y = v.Y }).ToList()
                }
            ],
            Obstacles = room.Geometry.Obstacles.Count > 0
                ? room.Geometry.Obstacles.Select((obs, idx) => new RoomPolygonYamlDto
                {
                    Id = $"obstacle-{idx + 1}",
                    Points = obs.Vertices.Select(v => new RoomPointYamlDto { X = v.X, Y = v.Y }).ToList()
                }).ToList()
                : null,
            Exits = room.Exits.Count > 0
                ? room.Exits.Select(e => new RoomExitYamlDto
                {
                    Id = e.Id,
                    Position = new RoomPointYamlDto { X = e.Position.X, Y = e.Position.Y },
                    Radius = e.Radius,
                    Bounds = e.Bounds != null ? new RoomRectYamlDto
                    {
                        X = e.Bounds.X,
                        Y = e.Bounds.Y,
                        Width = e.Bounds.Width,
                        Height = e.Bounds.Height
                    } : null,
                    Label = e.Label,
                    Target = new RoomExitTargetYamlDto
                    {
                        Room = e.TargetRoomId,
                        Spawn = e.TargetSpawnId
                    }
                }).ToList()
                : null,
            Notes = room.Notes,
            Objects = room.Objects?.Select(o => new RoomObjectYamlDto { Id = o.Id, Image = o.Image, Position = new RoomPointYamlDto { X = o.Position.X, Y = o.Position.Y }, Size = new RoomSizeYamlDto { Width = o.Size.Width, Height = o.Size.Height }, DepthY = o.DepthY }).ToList(),
            Perspective = room.Perspective == "adventure" ? null : room.Perspective,
            DepthScale = room.DepthScale,
            ActorHeight = room.ActorHeight == 64 ? null : room.ActorHeight,
            WalkSpeed = room.WalkSpeed == 130 ? null : room.WalkSpeed,
            Viewport = room.Viewport is null ? null : new RoomViewportYamlDto { Width = room.Viewport.Width, Height = room.Viewport.Height, ScrollMode = room.Viewport.ScrollMode }
        };
    }
}

public sealed class RoomSizeYamlDto
{
    [YamlMember(Alias = "width")]
    public int Width { get; set; } = 640;

    [YamlMember(Alias = "height")]
    public int Height { get; set; } = 360;
}

public sealed class RoomPointYamlDto
{
    [YamlMember(Alias = "x")]
    public double X { get; set; }

    [YamlMember(Alias = "y")]
    public double Y { get; set; }
}

public sealed class RoomSpawnYamlDto
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    [YamlMember(Alias = "position")]
    public RoomPointYamlDto Position { get; set; } = new();
}

public sealed class RoomPolygonYamlDto
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    [YamlMember(Alias = "points")]
    public List<RoomPointYamlDto> Points { get; set; } = new();
}

public sealed class RoomRectYamlDto
{
    [YamlMember(Alias = "x")]
    public double X { get; set; }

    [YamlMember(Alias = "y")]
    public double Y { get; set; }

    [YamlMember(Alias = "width")]
    public double Width { get; set; }

    [YamlMember(Alias = "height")]
    public double Height { get; set; }
}

public sealed class RoomExitTargetYamlDto
{
    [YamlMember(Alias = "room")]
    public string Room { get; set; } = string.Empty;

    [YamlMember(Alias = "spawn")]
    public string Spawn { get; set; } = string.Empty;
}

public sealed class RoomExitYamlDto
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    [YamlMember(Alias = "position")]
    public RoomPointYamlDto Position { get; set; } = new();

    [YamlMember(Alias = "radius")]
    public double Radius { get; set; } = 30;

    [YamlMember(Alias = "bounds")]
    public RoomRectYamlDto? Bounds { get; set; }

    [YamlMember(Alias = "label")]
    public string? Label { get; set; }

    [YamlMember(Alias = "target")]
    public RoomExitTargetYamlDto Target { get; set; } = new();
}

public sealed class RoomViewportYamlDto
{
    public int Width { get; set; }
    public int Height { get; set; }
    public string ScrollMode { get; set; } = string.Empty;
}

public sealed class RoomObjectYamlDto
{
 public string Id { get; set; } = "";
 public string Image { get; set; } = "";
 public RoomPointYamlDto Position { get; set; } = new();
 public RoomSizeYamlDto Size { get; set; } = new();
 public double DepthY { get; set; }
}
