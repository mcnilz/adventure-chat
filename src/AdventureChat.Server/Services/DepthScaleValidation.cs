using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

internal static class DepthScaleValidation
{
    public static void Validate(RoomYamlDto room, bool supported, List<string> errors)
    {
        if (room.DepthScale is not { } scale) return;
        if (!supported) errors.Add("depthScale requires world format 1.5.0.");
        if (scale.Back is null || scale.Front is null)
        {
            errors.Add("depthScale needs back and front reference lines.");
            return;
        }
        if (!double.IsFinite(scale.Back.Y) || !double.IsFinite(scale.Front.Y) ||
            scale.Back.Y < 0 || scale.Front.Y > room.Size.Height || scale.Back.Y >= scale.Front.Y)
            errors.Add("depthScale: back.y must be above front.y, both inside the room.");
        foreach (var line in new[] { scale.Back, scale.Front })
            if (!double.IsFinite(line.Factor) || line.Factor < 0.25 || line.Factor > 2)
                errors.Add("depthScale factors must be finite and between 0.25 and 2.");
    }
}
