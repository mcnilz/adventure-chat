using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

public interface IPathfindingService
{
    PathResult CalculatePath(RoomGeometry geometry, Point2D start, Point2D target);
}
