using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

// An isolated set of authoring drafts, never registered in a live world.
internal sealed class PreviewRoomService(IReadOnlyList<RoomDefinition> definitions) : IRoomService
{
    public RoomDefinition? GetRoom(string roomId) => definitions.FirstOrDefault(r => string.Equals(r.Id, roomId, StringComparison.OrdinalIgnoreCase));
    public IReadOnlyList<RoomDefinition> GetAllRooms() => definitions;
    public void RegisterOrUpdateRoom(RoomDefinition room) => throw new NotSupportedException();
    public void ReloadRooms() => throw new NotSupportedException();
}
