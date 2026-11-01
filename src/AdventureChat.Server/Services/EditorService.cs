using System.Text.RegularExpressions;
using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

public sealed record RoomSummaryDto(string Id, string Name, string Background, int Width, int Height);

public sealed record SaveRoomResult(bool Success, IReadOnlyList<string> Errors, string? RoomId = null);

public sealed record BackgroundItemDto(string Name, string Path, long SizeBytes, DateTimeOffset LastModified);

public sealed record BackgroundsOverviewDto(string FilesystemDirectory, string RelativePath, IReadOnlyList<BackgroundItemDto> Backgrounds);

public sealed record UploadBackgroundResult(bool Success, string? Name = null, string? Path = null, string? Error = null);

public interface IEditorService
{
    bool IsEditorEnabled { get; }
    IReadOnlyList<RoomSummaryDto> ListRooms();
    RoomYamlDto? GetRoom(string roomId);
    string? GetRoomRawYaml(string roomId);
    ValidationResult ValidateRoom(RoomYamlDto dto);
    ValidationResult ValidateWorld();
    ValidationResult ValidateWorld(IReadOnlyList<RoomYamlDto> rooms);
    SaveRoomResult SaveRoom(RoomYamlDto dto);
    BackgroundsOverviewDto ListBackgrounds();
    UploadBackgroundResult SaveBackground(string fileName, Stream content);
}

public sealed class EditorService : IEditorService
{
    private readonly IContentPathProvider _pathProvider;
    private readonly IRoomYamlSerializer _yamlSerializer;
    private readonly IRoomValidator _validator;
    private readonly IRoomService _roomService;
    private readonly bool _editorEnabled;

    public EditorService(
        IContentPathProvider pathProvider,
        IRoomYamlSerializer yamlSerializer,
        IRoomValidator validator,
        IRoomService roomService,
        bool editorEnabled = true)
    {
        _pathProvider = pathProvider;
        _yamlSerializer = yamlSerializer;
        _validator = validator;
        _roomService = roomService;
        _editorEnabled = editorEnabled;
    }

    public bool IsEditorEnabled => _editorEnabled;

    public IReadOnlyList<RoomSummaryDto> ListRooms()
    {
        var summaries = new List<RoomSummaryDto>();
        var roomsDir = _pathProvider.GetRoomsDirectory();

        if (Directory.Exists(roomsDir))
        {
            var files = Directory.GetFiles(roomsDir, "*.yaml");
            foreach (var file in files)
            {
                try
                {
                    var yaml = File.ReadAllText(file);
                    var dto = _yamlSerializer.Deserialize(yaml);
                    summaries.Add(new RoomSummaryDto(dto.Id, dto.Name, dto.Background, dto.Size.Width, dto.Size.Height));
                }
                catch
                {
                    // Ignore corrupted or non-parsable files during listing
                }
            }
        }

        // If no files on disk yet, fallback to registered in-memory rooms
        if (summaries.Count == 0)
        {
            foreach (var room in _roomService.GetAllRooms())
            {
                summaries.Add(new RoomSummaryDto(room.Id, room.Name, room.BackgroundUrl, room.Width, room.Height));
            }
        }

        return summaries;
    }

    public RoomYamlDto? GetRoom(string roomId)
    {
        var targetFile = _pathProvider.GetRoomFilePath(roomId);
        if (File.Exists(targetFile))
        {
            var yaml = File.ReadAllText(targetFile);
            return _yamlSerializer.Deserialize(yaml);
        }

        var memoryRoom = _roomService.GetRoom(roomId);
        return memoryRoom is not null ? RoomYamlDto.FromRoomDefinition(memoryRoom) : null;
    }

    public string? GetRoomRawYaml(string roomId)
    {
        var targetFile = _pathProvider.GetRoomFilePath(roomId);
        if (File.Exists(targetFile))
        {
            return File.ReadAllText(targetFile);
        }

        var memoryRoom = _roomService.GetRoom(roomId);
        if (memoryRoom is not null)
        {
            var dto = RoomYamlDto.FromRoomDefinition(memoryRoom);
            return _yamlSerializer.Serialize(dto);
        }

        return null;
    }

    public ValidationResult ValidateRoom(RoomYamlDto dto)
    {
        return _validator.Validate(dto);
    }

    public ValidationResult ValidateWorld()
    {
        var summaries = ListRooms();
        var roomDtos = new List<RoomYamlDto>();
        foreach (var summary in summaries)
        {
            var dto = GetRoom(summary.Id);
            if (dto is not null)
            {
                roomDtos.Add(dto);
            }
        }
        return _validator.ValidateWorld(roomDtos);
    }

    public ValidationResult ValidateWorld(IReadOnlyList<RoomYamlDto> rooms)
    {
        return _validator.ValidateWorld(rooms);
    }

    public SaveRoomResult SaveRoom(RoomYamlDto dto)
    {
        if (!_editorEnabled)
        {
            return new SaveRoomResult(false, ["Authoring / editor mode is disabled on this server instance."]);
        }

        var validation = _validator.Validate(dto);
        if (!validation.IsValid)
        {
            return new SaveRoomResult(false, validation.Errors);
        }

        var targetFile = _pathProvider.GetRoomFilePath(dto.Id);
        var canonicalYaml = _yamlSerializer.Serialize(dto);

        var tempFile = targetFile + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(tempFile, canonicalYaml);
            // Atomic replace of the destination file
            File.Move(tempFile, targetFile, overwrite: true);

            // Update in-memory room service
            _roomService.RegisterOrUpdateRoom(dto.ToRoomDefinition());

            return new SaveRoomResult(true, Array.Empty<string>(), dto.Id);
        }
        catch (Exception ex)
        {
            if (File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { /* ignore */ }
            }
            return new SaveRoomResult(false, [$"Failed to save room: {ex.Message}"]);
        }
    }

    public BackgroundsOverviewDto ListBackgrounds()
    {
        var dir = _pathProvider.GetBackgroundsDirectory();
        var rel = _pathProvider.GetBackgroundsRelativePath();
        var items = new List<BackgroundItemDto>();

        if (Directory.Exists(dir))
        {
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".png", ".jpg", ".jpeg", ".webp", ".gif", ".svg"
            };

            var files = Directory.GetFiles(dir);
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (allowedExtensions.Contains(ext))
                {
                    var fileInfo = new FileInfo(file);
                    var name = fileInfo.Name;
                    var path = $"{rel}/{name}";
                    items.Add(new BackgroundItemDto(name, path, fileInfo.Length, fileInfo.LastWriteTimeUtc));
                }
            }
        }

        items.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

        return new BackgroundsOverviewDto(
            FilesystemDirectory: Path.GetFullPath(dir),
            RelativePath: rel,
            Backgrounds: items
        );
    }

    public UploadBackgroundResult SaveBackground(string fileName, Stream content)
    {
        if (!_editorEnabled)
        {
            return new UploadBackgroundResult(false, Error: "Authoring / editor mode is disabled on this server instance.");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return new UploadBackgroundResult(false, Error: "Dateiname darf nicht leer sein.");
        }

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext != ".png")
        {
            return new UploadBackgroundResult(false, Error: "Nur PNG-Dateien sind erlaubt.");
        }

        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int count;
        while ((count = content.Read(chunk, 0, chunk.Length)) > 0)
        {
            if (buffer.Length + count > 10 * 1024 * 1024)
                return new UploadBackgroundResult(false, Error: "Die Datei überschreitet 10 MB.");
            buffer.Write(chunk, 0, count);
        }
        byte[] signature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
        if (buffer.Length < signature.Length || !buffer.GetBuffer().AsSpan(0, signature.Length).SequenceEqual(signature))
            return new UploadBackgroundResult(false, Error: "Ungültige PNG-Signatur.");
        buffer.Position = 0;

        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var sanitizedBase = Regex.Replace(nameWithoutExt, @"[^a-zA-Z0-9_-]", "-").Trim('-');
        if (string.IsNullOrEmpty(sanitizedBase))
        {
            sanitizedBase = "background-" + Guid.NewGuid().ToString("N")[..8];
        }

        var sanitizedFileName = sanitizedBase + ext;
        var dir = _pathProvider.GetBackgroundsDirectory();
        Directory.CreateDirectory(dir);

        var targetFile = Path.GetFullPath(Path.Combine(dir, sanitizedFileName));

        // Guard against Path Traversal
        if (!targetFile.StartsWith(Path.GetFullPath(dir), StringComparison.OrdinalIgnoreCase))
        {
            return new UploadBackgroundResult(false, Error: "Ungültiger Dateipfad.");
        }

        try
        {
            using (var fileStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                buffer.CopyTo(fileStream);
            }

            var rel = _pathProvider.GetBackgroundsRelativePath();
            return new UploadBackgroundResult(true, Name: sanitizedFileName, Path: $"{rel}/{sanitizedFileName}");
        }
        catch (Exception ex)
        {
            return new UploadBackgroundResult(false, Error: $"Fehler beim Speichern des Hintergrundbildes: {ex.Message}");
        }
    }
}
