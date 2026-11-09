using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AdventureChat.Server.Models;
using Json.Schema;

namespace AdventureChat.Server.Services;

public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static ValidationResult Success() => new(true, Array.Empty<string>());
    public static ValidationResult Failure(params string[] errors) => new(false, errors);
    public static ValidationResult Failure(IEnumerable<string> errors) => new(false, errors.ToList());
}

public interface IRoomValidator
{
    ValidationResult Validate(RoomYamlDto dto);
    ValidationResult ValidateWorld(IReadOnlyList<RoomYamlDto> rooms);
    ValidationResult ValidateJson(string json);
}

public sealed partial class RoomValidator : IRoomValidator
{
    [GeneratedRegex("^[a-zA-Z0-9_-]+$")]
    private static partial Regex SafeIdRegex();

    private readonly JsonSchema? _jsonSchema;
    private readonly bool _supportsDisplay;
    private readonly bool _supportsViewport;
    private readonly bool _supportsObjects;
    private readonly bool _supportsDepthScale;
    private readonly IPathfindingService _pathfinder;

    public RoomValidator(string? schemaPath = null, IPathfindingService? pathfinder = null)
    {
        _pathfinder = pathfinder ?? new PathfindingService();
        if (!string.IsNullOrEmpty(schemaPath) && File.Exists(schemaPath))
        {
            var schemaText = File.ReadAllText(schemaPath);
            _jsonSchema = JsonSchema.FromText(schemaText);
            _supportsDepthScale = JsonNode.Parse(schemaText)?["properties"]?["depthScale"] is not null;
            _supportsObjects = JsonNode.Parse(schemaText)?["properties"]?["objects"] is not null;
            _supportsViewport = JsonNode.Parse(schemaText)?["properties"]?["viewport"] is not null;
            _supportsDisplay = JsonNode.Parse(schemaText)?["properties"]?["perspective"] is not null;
        }
    }

    public ValidationResult ValidateJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return ValidationResult.Failure("JSON payload is empty.");
        }

        var errors = new List<string>();

        if (_jsonSchema is not null)
        {
            try
            {
                var jsonNode = JsonNode.Parse(json);
                var evalResult = _jsonSchema.Evaluate(jsonNode, new EvaluationOptions
                {
                    OutputFormat = OutputFormat.List
                });

                if (!evalResult.IsValid)
                {
                    if (evalResult.HasDetails)
                    {
                        foreach (var detail in evalResult.Details.Where(d => d.HasErrors && d.Errors != null))
                        {
                            foreach (var err in detail.Errors!)
                            {
                                errors.Add($"{detail.InstanceLocation}: {err.Value}");
                            }
                        }
                    }
                    else
                    {
                        errors.Add("JSON schema validation failed.");
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"JSON parsing error: {ex.Message}");
            }
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }

    public ValidationResult Validate(RoomYamlDto dto)
    {
        if (dto is null)
        {
            return ValidationResult.Failure("Room definition is null.");
        }

        var errors = new List<string>();

        // 1. Version
        if (dto.Version != 1)
        {
            errors.Add($"Unsupported version '{dto.Version}'. Expected version 1.");
        }

        if (!_supportsDisplay && (dto.Perspective is not null || dto.ActorHeight is not null || dto.WalkSpeed is not null))
            errors.Add("Display settings require world format 1.1.0; older schemas cannot save these fields.");
        if (dto.Perspective is not (null or "adventure" or "top-down")) errors.Add("Unknown perspective; use adventure or top-down.");
        if (dto.ActorHeight is double height && (!double.IsFinite(height) || height < 16 || height > 128)) errors.Add("actorHeight must be between 16 and 128 logical pixels.");
        if (dto.WalkSpeed is double speed && (!double.IsFinite(speed) || speed < 1 || speed > 1000)) errors.Add("walkSpeed must be between 1 and 1000 logical pixels per second.");

        if (dto.Viewport is { } viewport)
        {
            if (!_supportsViewport) errors.Add("viewport requires world format 1.2.0.");
            if (viewport.Width <= 0 || viewport.Height != dto.Size.Height || viewport.ScrollMode != "horizontal")
                errors.Add("viewport requires positive width, room height and horizontal scrollMode.");
        }

        RoomObjectValidation.Validate(dto, _supportsObjects, errors);
        DepthScaleValidation.Validate(dto, _supportsDepthScale, errors);

        // 2. ID
        if (string.IsNullOrWhiteSpace(dto.Id))
        {
            errors.Add("Room id must not be empty.");
        }
        else if (!SafeIdRegex().IsMatch(dto.Id))
        {
            errors.Add($"Room id '{dto.Id}' contains invalid characters. Only alphanumeric, dashes and underscores are allowed.");
        }

        // 3. Name
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            errors.Add("Room name must not be empty.");
        }
        else if (dto.Name.Length > 100)
        {
            errors.Add("Room name must not exceed 100 characters.");
        }

        // 4. Background
        if (string.IsNullOrWhiteSpace(dto.Background))
        {
            errors.Add("Room background image path must not be empty.");
        }

        // 5. Size
        if (dto.Size.Width <= 0 || dto.Size.Height <= 0)
        {
            errors.Add($"Room size ({dto.Size.Width}x{dto.Size.Height}) must have positive width and height.");
        }

        // 6. Walkable areas
        if (dto.Walkable is null || dto.Walkable.Count == 0)
        {
            errors.Add("Room must define at least one walkable polygon.");
        }
        else
        {
            foreach (var w in dto.Walkable)
            {
                ValidatePolygon(w.Points, $"Walkable area '{w.Id}'", errors);
            }
        }

        // 7. Obstacles
        if (dto.Obstacles is not null)
        {
            foreach (var obs in dto.Obstacles)
            {
                ValidatePolygon(obs.Points, $"Obstacle '{obs.Id}'", errors);
            }
        }

        // 8. Geometry checks if walkable is valid
        if (errors.Count == 0)
        {
            var roomDef = dto.ToRoomDefinition();

            // Obstacles must be inside outer boundary
            if (dto.Obstacles is not null)
            {
                foreach (var obs in roomDef.Geometry.Obstacles)
                {
                    foreach (var v in obs.Vertices)
                    {
                        if (!roomDef.Geometry.OuterBoundary.ContainsPoint(v))
                        {
                            errors.Add($"Obstacle vertex ({v.X}, {v.Y}) is outside the walkable boundary.");
                            break;
                        }
                    }
                }
            }

            // 9. Spawns
            if (dto.Spawns is null || dto.Spawns.Count == 0)
            {
                errors.Add("Room must define at least one spawn point.");
            }
            else
            {
                var spawnIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var s in dto.Spawns)
                {
                    if (string.IsNullOrWhiteSpace(s.Id) || !SafeIdRegex().IsMatch(s.Id))
                    {
                        errors.Add($"Spawn id '{s.Id}' is invalid.");
                    }
                    else if (!spawnIds.Add(s.Id))
                    {
                        errors.Add($"Duplicate spawn id '{s.Id}'.");
                    }

                    var p = new Point2D(s.Position.X, s.Position.Y);
                    if (!roomDef.Geometry.IsWalkable(p))
                    {
                        errors.Add($"Spawn point '{s.Id}' at ({p.X}, {p.Y}) is not located on a walkable area.");
                    }
                }
            }

            // 10. Exits
            if (dto.Exits is not null)
            {
                var exitIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in dto.Exits)
                {
                    if (string.IsNullOrWhiteSpace(e.Id) || !SafeIdRegex().IsMatch(e.Id))
                    {
                        errors.Add($"Exit id '{e.Id}' is invalid.");
                    }
                    else if (!exitIds.Add(e.Id))
                    {
                        errors.Add($"Duplicate exit id '{e.Id}'.");
                    }

                    if (string.IsNullOrWhiteSpace(e.Target.Room) || !SafeIdRegex().IsMatch(e.Target.Room))
                    {
                        errors.Add($"Exit '{e.Id}' target room '{e.Target.Room}' is invalid.");
                    }

                    if (string.IsNullOrWhiteSpace(e.Target.Spawn) || !SafeIdRegex().IsMatch(e.Target.Spawn))
                    {
                        errors.Add($"Exit '{e.Id}' target spawn '{e.Target.Spawn}' is invalid.");
                    }

                    var p = new Point2D(e.Position.X, e.Position.Y);
                    if (!roomDef.Geometry.IsWalkable(p))
                    {
                        errors.Add($"Exit '{e.Id}' trigger position at ({p.X}, {p.Y}) is not located on a walkable area.");
                    }
                }

                // 11. Exit reachability check from room spawns
                var validSpawns = roomDef.Spawns
                    .Where(s => roomDef.Geometry.IsWalkable(s.Position))
                    .ToList();

                if (validSpawns.Count > 0)
                {
                    foreach (var e in dto.Exits)
                    {
                        var exitPos = new Point2D(e.Position.X, e.Position.Y);
                        if (roomDef.Geometry.IsWalkable(exitPos))
                        {
                            bool isReachable = validSpawns.Any(s => _pathfinder.CalculatePath(roomDef.Geometry, s.Position, exitPos).Success);
                            if (!isReachable)
                            {
                                errors.Add($"Exit '{e.Id}' at ({exitPos.X}, {exitPos.Y}) is unreachable from any spawn point in this room.");
                            }
                        }
                    }
                }
            }
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }

    public ValidationResult ValidateWorld(IReadOnlyList<RoomYamlDto> rooms)
    {
        if (rooms is null || rooms.Count == 0)
        {
            return ValidationResult.Failure("World contains no room definitions.");
        }

        var errors = new List<string>();
        var roomMap = new Dictionary<string, RoomYamlDto>(StringComparer.OrdinalIgnoreCase);

        // 1. Individual room validations and duplicate ID check
        foreach (var room in rooms)
        {
            if (string.IsNullOrWhiteSpace(room.Id))
            {
                errors.Add("Encountered room definition with empty ID.");
                continue;
            }

            if (roomMap.ContainsKey(room.Id))
            {
                errors.Add($"Duplicate room ID '{room.Id}' found in world.");
                continue;
            }

            roomMap[room.Id] = room;

            var singleResult = Validate(room);
            if (!singleResult.IsValid)
            {
                foreach (var err in singleResult.Errors)
                {
                    errors.Add($"Room '{room.Id}': {err}");
                }
            }
        }

        // 2. Cross-room exit and spawn reference validation
        foreach (var room in rooms)
        {
            if (room.Exits is null) continue;

            foreach (var exit in room.Exits)
            {
                if (string.IsNullOrWhiteSpace(exit.Target.Room))
                {
                    errors.Add($"Room '{room.Id}': Exit '{exit.Id}' has empty target room.");
                    continue;
                }

                if (!roomMap.TryGetValue(exit.Target.Room, out var targetRoom))
                {
                    errors.Add($"Room '{room.Id}': Exit '{exit.Id}' references non-existent target room '{exit.Target.Room}'.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(exit.Target.Spawn))
                {
                    errors.Add($"Room '{room.Id}': Exit '{exit.Id}' has empty target spawn for room '{exit.Target.Room}'.");
                    continue;
                }

                var targetSpawnExists = targetRoom.Spawns?.Any(s => string.Equals(s.Id, exit.Target.Spawn, StringComparison.OrdinalIgnoreCase)) ?? false;
                if (!targetSpawnExists)
                {
                    errors.Add($"Room '{room.Id}': Exit '{exit.Id}' references non-existent target spawn '{exit.Target.Spawn}' in room '{exit.Target.Room}'.");
                }
            }
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }

    private static void ValidatePolygon(IReadOnlyList<RoomPointYamlDto> points, string polyName, List<string> errors)
    {
        if (points is null || points.Count < 3)
        {
            errors.Add($"{polyName} must have at least 3 vertices (found {points?.Count ?? 0}).");
            return;
        }

        // Check for non-finite coordinates
        for (int i = 0; i < points.Count; i++)
        {
            if (double.IsNaN(points[i].X) || double.IsInfinity(points[i].X) ||
                double.IsNaN(points[i].Y) || double.IsInfinity(points[i].Y))
            {
                errors.Add($"{polyName} contains invalid non-finite coordinates at vertex {i}.");
                return;
            }
        }

        // Check for duplicate consecutive vertices
        for (int i = 0; i < points.Count; i++)
        {
            var next = (i + 1) % points.Count;
            if (Math.Abs(points[i].X - points[next].X) < 1e-6 && Math.Abs(points[i].Y - points[next].Y) < 1e-6)
            {
                errors.Add($"{polyName} contains duplicate consecutive vertices at indices {i} and {next}.");
                return;
            }
        }

        // Check for self-intersections
        int n = points.Count;
        for (int i = 0; i < n; i++)
        {
            var p1 = new Point2D(points[i].X, points[i].Y);
            var p2 = new Point2D(points[(i + 1) % n].X, points[(i + 1) % n].Y);

            for (int j = i + 1; j < n; j++)
            {
                // Skip adjacent edges
                if (j == i || j == (i + 1) % n || (i == 0 && j == n - 1))
                {
                    continue;
                }

                var p3 = new Point2D(points[j].X, points[j].Y);
                var p4 = new Point2D(points[(j + 1) % n].X, points[(j + 1) % n].Y);

                if (SegmentsIntersectStrict(p1, p2, p3, p4))
                {
                    errors.Add($"{polyName} is self-intersecting (edge {i}->{(i+1)%n} crosses edge {j}->{(j+1)%n}).");
                    return;
                }
            }
        }

        // Check non-zero area
        double area = 0.0;
        for (int i = 0; i < n; i++)
        {
            var j = (i + 1) % n;
            area += points[i].X * points[j].Y - points[j].X * points[i].Y;
        }

        if (Math.Abs(area) < 1e-4)
        {
            errors.Add($"{polyName} is degenerate (area is zero).");
        }
    }

    private static bool SegmentsIntersectStrict(Point2D p1, Point2D p2, Point2D p3, Point2D p4)
    {
        double d1 = Direction(p3, p4, p1);
        double d2 = Direction(p3, p4, p2);
        double d3 = Direction(p1, p2, p3);
        double d4 = Direction(p1, p2, p4);

        if (((d1 > 1e-6 && d2 < -1e-6) || (d1 < -1e-6 && d2 > 1e-6)) &&
            ((d3 > 1e-6 && d4 < -1e-6) || (d3 < -1e-6 && d4 > 1e-6)))
        {
            return true;
        }

        return false;
    }

    private static double Direction(Point2D pi, Point2D pj, Point2D pk)
    {
        return (pk.X - pi.X) * (pj.Y - pi.Y) - (pj.X - pi.X) * (pk.Y - pi.Y);
    }
}
