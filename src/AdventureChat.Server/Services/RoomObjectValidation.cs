using System.Buffers.Binary;
using System.Text.RegularExpressions;
using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

internal static class RoomObjectValidation
{
    public static void Validate(RoomYamlDto room, bool supported, List<string> errors)
    {
        if (room.Objects is null) return;
        if (!supported) errors.Add("objects requires world format 1.4.0.");
        if (room.Objects.Count > 128) errors.Add("objects allows at most 128 objects.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var obj in room.Objects)
        {
            if (obj is null || obj.Id is null || !Regex.IsMatch(obj.Id, "^[a-zA-Z0-9_-]{1,64}$") || !ids.Add(obj.Id))
            { errors.Add("objects requires unique valid IDs (1–64 ASCII letters, digits, _ or -)."); continue; }
            bool Integer(double v) => double.IsFinite(v) && v == Math.Truncate(v);
            if (obj.Position is null || obj.Size is null || obj.Size.Width < 1 || obj.Size.Height < 1 ||
                !Integer(obj.Position.X) || !Integer(obj.Position.Y) || obj.Position.X < 0 || obj.Position.Y < 0 ||
                obj.Position.X + obj.Size.Width > room.Size.Width || obj.Position.Y + obj.Size.Height > room.Size.Height ||
                !Integer(obj.DepthY) || obj.DepthY < 0 || obj.DepthY > room.Size.Height)
                errors.Add($"Object '{obj.Id}': integer position/positive size must fit inside room; depthY must be within room height.");
            if (string.IsNullOrWhiteSpace(obj.Image) || !obj.Image.StartsWith("assets/images/", StringComparison.Ordinal) ||
                !obj.Image.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                errors.Add($"Object '{obj.Id}': image must be a PNG under assets/images.");
        }
    }

    public static void ValidateImages(string root, IReadOnlyList<RoomObjectYamlDto>? objects)
    {
        foreach (var obj in objects ?? [])
        {
            try
            {
                var file = WorldPackage.SafePath(root, obj.Image, true);
                using var stream = File.OpenRead(file);
                var header = new byte[24];
                stream.ReadExactly(header);
                var width = checked((int)BinaryPrimitives.ReadUInt32BigEndian(header[16..20]));
                var height = checked((int)BinaryPrimitives.ReadUInt32BigEndian(header[20..24]));
                if (width <= 0 || height <= 0) throw new InvalidOperationException("Invalid dimensions.");
                PngValidator.Validate(file, width, height);
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or OverflowException)
            { throw new InvalidOperationException($"Object '{obj.Id}', image '{obj.Image}': {ex.Message}", ex); }
        }
    }
}
