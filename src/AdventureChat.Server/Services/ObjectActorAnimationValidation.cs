using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

internal static class ObjectActorAnimationValidation
{
    public static void Validate(RoomYamlDto room, bool supported, List<string> errors)
    {
        foreach (var obj in room.Objects ?? [])
        {
            if (obj.Interaction is not { } interaction) continue;
            var points = obj.UsePoints?.Count ?? (interaction.UsePoint is null ? 0 : 1);
            foreach (var action in interaction.Actions)
            {
                if (action.ActorAnimation is not { } animation) continue;
                var subject = $"Object '{obj.Id}', action '{action.Id}'";
                if (!supported)
                    errors.Add($"{subject}: actorAnimation requires format 1.10.0.");
                if (animation.Point < 0 || animation.Point >= points)
                    errors.Add($"{subject}: actorAnimation must select an existing use point.");
                if (animation.Direction is not ("up" or "down" or "left" or "right"))
                    errors.Add($"{subject}: actorAnimation requires a valid direction.");
                if (animation.DurationMs is < 200 or > 5000)
                    errors.Add($"{subject}: actorAnimation duration must be 200–5000 ms.");
            }
        }
    }
}
