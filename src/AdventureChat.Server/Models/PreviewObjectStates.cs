namespace AdventureChat.Server.Models;

public sealed record PreviewObjectStates(string RoomId, IReadOnlyList<ObjectStateDto> States);
