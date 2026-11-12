using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

internal static class ObjectInteractionValidation
{
    public static void ValidateReachability(RoomDefinition room, IPathfindingService pathfinder, List<string> errors)
    {
        foreach (var obj in room.Objects ?? [])
        {
            if (obj.Interaction is not { } interaction) continue;
            var point = new Point2D(interaction.UsePoint.X, interaction.UsePoint.Y);
            if (room.Geometry.IsWalkable(point) && !room.Spawns.Any(spawn => pathfinder.CalculatePath(room.Geometry, spawn.Position, point).Success))
                errors.Add($"Object '{obj.Id}': use point is unreachable from every spawn.");
        }
    }

    public static void ValidateRoom(RoomYamlDto room, bool supported, List<string> errors)
    {
        foreach (var obj in room.Objects ?? [])
        {
            if (obj?.Interaction is not { } interaction) continue;
            if (!supported) errors.Add($"Object '{obj.Id}': interaction requires format 1.6.0.");
            if (interaction.States is null || interaction.Actions is null || interaction.UsePoint is null)
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
            var point = new Point2D(interaction.UsePoint.X, interaction.UsePoint.Y);
            if (!point.IsFinite() || !double.IsFinite(interaction.UseRadius) || interaction.UseRadius <= 0 || interaction.UseRadius > 200 ||
                (room.Walkable?.FirstOrDefault()?.Points is { Count: >= 3 } && !room.ToRoomDefinition().Geometry.IsWalkable(point)))
                errors.Add($"Object '{obj.Id}': use point must be walkable and radius within 0–200.");
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
