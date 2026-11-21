using System.Text.RegularExpressions;
using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

internal static partial class NpcValidation
{
    internal const double TalkDistance = 48;

    internal static void Validate(RoomDefinition room, bool supported, bool supportsConditions, List<string> errors)
    {
        if (room.Npcs is null) return;
        if (!supported)
        {
            errors.Add("npcs requires world format 1.11.0.");
            return;
        }

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var npc in room.Npcs)
        {
            if (npc is null || !AddId(ids, npc.Id))
            {
                errors.Add($"Invalid or duplicate NPC id '{npc?.Id}'.");
                continue;
            }
            if (!HasText(npc.Name, 100) || !IsId(npc.CharacterId))
                errors.Add($"NPC '{npc.Id}' requires a name and a valid characterId.");
            if (npc.Position.DistanceTo(npc.TalkPoint) > TalkDistance ||
                !room.Geometry.HasLineOfSight(npc.TalkPoint, npc.Position, clearance: 0))
                errors.Add($"NPC '{npc.Id}' requires a walkable talkPoint within {TalkDistance} pixels and a clear line to its position.");
            ValidateOptions(room, npc, supportsConditions, errors);
        }
    }

    private static void ValidateOptions(RoomDefinition room, NpcDefinition npc, bool supportsConditions, List<string> errors)
    {
        if (npc.Options is not { Count: >= 2 and <= 8 })
        {
            errors.Add($"NPC '{npc.Id}' requires 2 to 8 dialogue options.");
            return;
        }
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var texts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in npc.Options)
        {
            if (option is null || !AddId(ids, option.Id) || !HasText(option.Text, 120) ||
                !texts.Add(option.Text.Trim()) || !HasText(option.Reply, 400))
            {
                errors.Add($"NPC '{npc.Id}' requires unique dialogue option IDs and statements, each with a reply.");
                continue;
            }
            if (option.When is null && option.Replies is null) continue;
            var label = $"NPC '{npc.Id}', dialogue option '{option.Id}'";
            if (!supportsConditions)
            {
                errors.Add($"{label}: dialogue conditions require world format 1.12.0.");
                continue;
            }
            if (option.When is { } condition) ValidateCondition(room, condition, label, errors);
            if (option.Replies is null) continue;
            if (option.Replies.Count is < 1 or > 8)
                errors.Add($"{label} requires 1 to 8 conditional replies.");
            foreach (var reply in option.Replies)
            {
                if (reply?.When is null || !HasText(reply.Reply, 400))
                    errors.Add($"{label}: every conditional reply requires a condition and a reply.");
                else
                    ValidateCondition(room, reply.When, label, errors);
            }
        }
    }

    private static void ValidateCondition(RoomDefinition room, DialogueCondition condition, string label, List<string> errors)
    {
        var interaction = room.Objects?.FirstOrDefault(obj => obj.Id == condition.Object)?.Interaction;
        if (interaction is null)
            errors.Add($"{label} references unknown interactive object '{condition.Object}' in room '{room.Id}'.");
        else if (!interaction.States.Any(state => state.Id == condition.State))
            errors.Add($"{label} references unknown state '{condition.State}' on object '{condition.Object}'.");
    }

    internal static void ValidateCharacters(IReadOnlyList<NpcDefinition>? npcs, IReadOnlyList<CharacterDefinition> characters)
    {
        foreach (var npc in npcs ?? [])
            if (!characters.Any(character => character.Id == npc.CharacterId))
                throw new InvalidOperationException($"NPC '{npc.Id}' references unknown character '{npc.CharacterId}'.");
    }

    private static bool AddId(HashSet<string> ids, string? id) => IsId(id) && ids.Add(id!);
    private static bool IsId(string? id) => id is { Length: <= 64 } && SafeId().IsMatch(id);
    private static bool HasText(string? text, int limit) => !string.IsNullOrWhiteSpace(text) && text.Length <= limit;

    [GeneratedRegex(@"\A[a-zA-Z0-9_-]+\z")]
    private static partial Regex SafeId();
}
