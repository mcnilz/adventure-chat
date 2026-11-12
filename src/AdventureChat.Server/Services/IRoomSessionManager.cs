using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

public record JoinResult(bool Success, string? Error, PlayerSession? Session, RoomSnapshotDto? Snapshot);
public record ChangeRoomResult(bool Success, string? Error, string? OldRoomId, PlayerSession? Session, RoomSnapshotDto? Snapshot);
public record MoveResult(bool Success, string? Error, string? SessionId, PointDto? Target, IReadOnlyList<PointDto>? Waypoints, string? RoomId = null, long Revision = 0);
public record SendChatMessageResult(bool Success, string? Error, ChatMessageDto? Message);
public record ObjectActionResult(bool Success, string? Error, IReadOnlyList<ObjectStatesChangedDto> Changes, string? ErrorCode = null);

public interface IRoomSessionManager
{
    JoinResult JoinRoom(string connectionId, string roomId, string? displayName, Point2D? initialPosition = null, string? characterId = null);
    ChangeRoomResult ChangeRoom(string connectionId, string exitId);
    RoomSnapshotDto? GetSnapshot(string connectionId, bool includeHistory = false);
    PlayerSession? Leave(string connectionId);
    PlayerSession? GetPlayer(string connectionId);
    IReadOnlyList<PlayerSession> GetPlayersInRoom(string roomId);
    MoveResult RequestMove(string connectionId, Point2D target, Point2D? currentPosition = null);
    void UpdatePlayerProgress(string connectionId, Point2D position, string direction, string state);
    SendChatMessageResult SendChatMessage(string connectionId, string text);
    IReadOnlyList<ChatMessageDto> GetRecentMessages(string roomId);
    ObjectActionResult RequestObjectAction(string connectionId, ObjectActionRequestDto request);
    SaveRoomResult SaveRoomAndReset(Func<SaveRoomResult> save);
}
