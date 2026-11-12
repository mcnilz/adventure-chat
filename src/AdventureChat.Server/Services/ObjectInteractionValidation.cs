using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

internal static class ObjectInteractionValidation
{
    public static void ValidateReachability(RoomDefinition room, IPathfindingService pathfinder, List<string> errors)
    {
        foreach (var obj in room.Objects ?? [])
        {
            var points = obj.UsePoints ?? (obj.Interaction?.UsePoint is { } old ? [new Point2D(old.X, old.Y)] : []);
            foreach (var point in points)
                if (room.Geometry.IsWalkable(point) && !room.Spawns.Any(spawn => pathfinder.CalculatePath(room.Geometry, spawn.Position, point).Success))
                    errors.Add($"Object '{obj.Id}': use point is unreachable from every spawn.");
        }
    }

    public static void ValidateRoom(RoomYamlDto room, bool supported, bool supportsUsePoints, List<string> errors)
    {
        var geometry = new RoomGeometry(
            new Polygon(room.Walkable[0].Points.Select(p => new Point2D(p.X, p.Y)).ToList()),
            (room.Obstacles ?? []).Select(o => new Polygon(o.Points.Select(p => new Point2D(p.X, p.Y)).ToList())).ToList());
        foreach (var obj in room.Objects ?? [])
        {
            if (obj is null) continue;
            if (obj.UsePoints is { } declared)
            {
                if (!supportsUsePoints) errors.Add($"Object '{obj.Id}': usePoints requires format 1.7.0.");
                if (declared.Count is < 1 or > 8 || declared.Any(p => p is null) || declared.Select(p => (p.X, p.Y)).Distinct().Count() != declared.Count)
                { errors.Add($"Object '{obj.Id}': usePoints requires 1–8 distinct points."); continue; }
            }
            var points = obj.UsePoints ?? (obj.Interaction?.UsePoint is { } old ? [old] : []);
            foreach (var candidate in points.Where(p => p is not null))
            {
                var point = new Point2D(candidate.X, candidate.Y);
                if (!point.IsFinite() || !geometry.IsWalkable(point))
                    errors.Add($"Object '{obj.Id}': use points must be walkable.");
            }
            if (obj.Interaction is not { } interaction) continue;
            if (!supported) errors.Add($"Object '{obj.Id}': interaction requires format 1.6.0.");
            if (interaction.States is null || interaction.Actions is null || points.Count == 0)
            { errors.Add($"Object '{obj.Id}': interaction is incomplete."); continue; }

            var states = new HashSet<string>(StringComparer.Ordinal);
            foreach (var state in interaction.States)
            {
                if (state is null || string.IsNullOrWhiteSpace(state.Id) || !states.Add(state.Id))
                { errors.Add($"Object '{obj.Id}': state IDs must be unique and nonempty."); continue; }
                if (state.Position is null || state.Size is null || state.Size.Width < 1 || state.Size.Height < 1 ||
                    !double.IsFinite(state.Position.X) || !double.IsFinite(state.Position.Y) ||
                    state.Position.X != Math.Truncate(state.Position.X) || state.Position.Y != Math.Truncate(state.Position.Y) ||
                    state.Position.X < 0 || state.Position.Y < 0 || state.Position.X + state.Size.Width > room.Size.Width ||
                    state.Position.Y + state.Size.Height > room.Size.Height || !double.IsFinite(state.DepthY) ||
                    state.DepthY != Math.Truncate(state.DepthY) || state.DepthY < 0 || state.DepthY > room.Size.Height)
                    errors.Add($"Object '{obj.Id}', state '{state.Id}': display must fit the room.");
            }
            if (!states.Contains(interaction.InitialState)) errors.Add($"Object '{obj.Id}': initial state does not exist.");
            var actions = new HashSet<(string, string)>();
            foreach (var action in interaction.Actions)
            {
                if (action is null || string.IsNullOrWhiteSpace(action.Id) || !actions.Add((action.Id, action.From)))
                { errors.Add($"Object '{obj.Id}': action and source state must be unique."); continue; }
                if (!states.Contains(action.From) || !states.Contains(action.To))
                    errors.Add($"Object '{obj.Id}', action '{action.Id}': unknown source or target state.");
            }
            if (!double.IsFinite(interaction.UseRadius) || interaction.UseRadius <= 0 || interaction.UseRadius > 200)
                errors.Add($"Object '{obj.Id}': use radius must be within 0–200.");
        }
    }

    public static void ValidateWorld(IReadOnlyList<RoomYamlDto> rooms, List<string> errors)
    {
        foreach (var room in rooms)
        foreach (var obj in room.Objects ?? [])
        foreach (var action in obj.Interaction?.Actions ?? [])
        {
            var assigned = new HashSet<(string, string)> { (room.Id, obj.Id) };
            foreach (var effect in action.Effects ?? [])
            {
                if (effect is null || !assigned.Add((effect.Room, effect.Object)))
                { errors.Add($"Object '{obj.Id}', action '{action.Id}': duplicate target."); continue; }
                var target = rooms.FirstOrDefault(r => r.Id == effect.Room)?.Objects?.FirstOrDefault(o => o.Id == effect.Object);
                if (target?.Interaction?.States?.Any(s => s.Id == effect.State) != true)
                    errors.Add($"Object '{obj.Id}', action '{action.Id}': unknown target or state.");
            }
        }
    }
}
