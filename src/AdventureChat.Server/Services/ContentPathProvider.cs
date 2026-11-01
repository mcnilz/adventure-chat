using System.Text.RegularExpressions;

namespace AdventureChat.Server.Services;

public interface IContentPathProvider
{
    string GetContentRoot();
    string GetRoomsDirectory();
    string GetRoomFilePath(string roomId);
    string GetSchemaFilePath();
}

public sealed partial class ContentPathProvider : IContentPathProvider
{
    [GeneratedRegex("^[a-zA-Z0-9_-]+$")]
    private static partial Regex SafeIdRegex();

    private readonly string _contentRoot;
    private readonly string _schemaPath;

    public ContentPathProvider(string? contentRoot = null, string? schemaPath = null)
    {
        if (!string.IsNullOrEmpty(contentRoot))
        {
            _contentRoot = Path.GetFullPath(contentRoot);
        }
        else
        {
            _contentRoot = ResolveDefaultContentRoot();
        }

        if (!string.IsNullOrEmpty(schemaPath))
        {
            _schemaPath = Path.GetFullPath(schemaPath);
        }
        else
        {
            _schemaPath = ResolveDefaultSchemaPath();
        }

        Directory.CreateDirectory(GetRoomsDirectory());
    }

    public string GetContentRoot() => _contentRoot;

    public string GetRoomsDirectory() => Path.Combine(_contentRoot, "rooms");

    public string GetSchemaFilePath() => _schemaPath;

    public string GetRoomFilePath(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId) || !SafeIdRegex().IsMatch(roomId))
        {
            throw new ArgumentException($"Invalid roomId '{roomId}'. Room IDs may only contain letters, digits, dashes, and underscores.", nameof(roomId));
        }

        var roomsDir = Path.GetFullPath(GetRoomsDirectory());
        var targetFile = Path.GetFullPath(Path.Combine(roomsDir, $"{roomId}.yaml"));

        // Guard against Path Traversal attacks
        if (!targetFile.StartsWith(roomsDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Access to path '{targetFile}' outside of content rooms directory is forbidden.");
        }

        return targetFile;
    }

    private static string ResolveDefaultContentRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var contentDir = Path.Combine(current, "content");
            if (Directory.Exists(contentDir))
            {
                return Path.GetFullPath(contentDir);
            }

            var marker = Path.Combine(current, "SPEC.md");
            if (File.Exists(marker))
            {
                var rootContent = Path.Combine(current, "content");
                Directory.CreateDirectory(rootContent);
                return Path.GetFullPath(rootContent);
            }

            current = Directory.GetParent(current)?.FullName;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "content"));
    }

    private static string ResolveDefaultSchemaPath()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var schemaFile = Path.Combine(current, "schemas", "room.schema.json");
            if (File.Exists(schemaFile))
            {
                return Path.GetFullPath(schemaFile);
            }

            current = Directory.GetParent(current)?.FullName;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "schemas", "room.schema.json"));
    }
}
