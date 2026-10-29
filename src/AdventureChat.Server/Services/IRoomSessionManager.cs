using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

public record JoinResult(bool Success, string? Error, PlayerSession? Session, RoomSnapshotDto? Snapshot);
public record MoveResult(bool Success, string? Error, string? SessionId, PointDto? Target, IReadOnlyList<PointDto>? Waypoints);

public interface IRoomSessionManager
{
    JoinResult JoinRoom(string connectionId, string roomId, string? displayName, Point2D? initialPosition = null);
    PlayerSession? Leave(string connectionId);
    PlayerSession? GetPlayer(string connectionId);
    IReadOnlyList<PlayerSession> GetPlayersInRoom(string roomId);
    MoveResult RequestMove(string connectionId, Point2D target, Point2D? currentPosition = null);
    void UpdatePlayerProgress(string connectionId, Point2D position, string direction, string state);
}
