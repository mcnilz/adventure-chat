using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

internal static class NpcReactionValidation
{
    public static void Validate(RoomDefinition room, bool supported, IPathfindingService paths, List<string> errors)
    {
        RoomGeometry? initialGeometry = null;
        var checkedNpcs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var obj in room.Objects ?? [])
        foreach (var action in obj.Interaction?.Actions ?? [])
        {
            if (action.NpcReaction is not { } reaction) continue;
            var label = $"Object '{obj.Id}', action '{action.Id}'";
            var npc = room.Npcs?.FirstOrDefault(candidate => candidate?.Id == reaction.Npc);
            if (!supported)
                errors.Add($"{label}: npcReaction requires format 1.15.0.");
            else if (npc is null)
                errors.Add($"{label}: npcReaction references unknown NPC '{reaction.Npc}' in this room.");
            else if (!paths.CalculatePath(room.Geometry, npc.Position, reaction.Position).Success)
                errors.Add($"{label}: npcReaction target is unreachable from NPC '{npc.Id}'.");
            else if (checkedNpcs.Add(npc.Id))
            {
                initialGeometry ??= new ObjectNavigation(new PreviewRoomService([room]), _ => []).Geometry(room);
                // Enabled exits in the initial geometry already require a path from a local spawn.
                if (!room.Spawns.Any(spawn => paths.CalculatePath(initialGeometry, npc.Position, spawn.Position).Success))
                    errors.Add($"{label}: NPC '{npc.Id}' has no reachable spawn in the initial state.");
            }
        }
    }
}
