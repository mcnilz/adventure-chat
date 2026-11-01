using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

public sealed record RoomSummaryDto(string Id, string Name, string Background, int Width, int Height);

public sealed record SaveRoomResult(bool Success, IReadOnlyList<string> Errors, string? RoomId = null);

public interface IEditorService
{
    bool IsEditorEnabled { get; }
    IReadOnlyList<RoomSummaryDto> ListRooms();
    RoomYamlDto? GetRoom(string roomId);
    string? GetRoomRawYaml(string roomId);
    ValidationResult ValidateRoom(RoomYamlDto dto);
    SaveRoomResult SaveRoom(RoomYamlDto dto);
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
}
