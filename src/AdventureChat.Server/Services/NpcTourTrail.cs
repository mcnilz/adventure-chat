using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

internal sealed record NpcTourTrail(IReadOnlyList<Point2D> Route, double JoinDistance)
{
    internal static NpcTourTrail? Plan(IPathfindingService pathfinder, RoomGeometry geometry,
        IReadOnlyList<Point2D> guideRoute, Point2D guest, double stopDistance)
    {
        var trail = TrimEnd(guideRoute, stopDistance);
        var start = trail[0];
        var join = trail.Count > 1 ? ClosestPoint(guest, start, trail[1]) : start;
        var approach = pathfinder.CalculatePath(geometry, guest, join);
        if (!approach.Success && join != start)
        {
            join = start;
            approach = pathfinder.CalculatePath(geometry, guest, join);
        }
        if (!approach.Success) return null;
        var remaining = trail.Skip(1).SkipWhile(point => point == join);
        return new([..approach.Waypoints!, ..remaining], start.DistanceTo(join));
    }

    private static List<Point2D> TrimEnd(IReadOnlyList<Point2D> route, double distance)
    {
        var points = route.ToList();
        while (points.Count > 1)
        {
            var from = points[^2];
            var end = points[^1];
            var length = from.DistanceTo(end);
            if (length > distance)
            {
                var remaining = (length - distance) / length;
                points[^1] = new(from.X + (end.X - from.X) * remaining, from.Y + (end.Y - from.Y) * remaining);
                break;
            }
            distance -= length;
            points.RemoveAt(points.Count - 1);
        }
        return points;
    }

    private static Point2D ClosestPoint(Point2D point, Point2D from, Point2D to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var lengthSquared = dx * dx + dy * dy;
        if (lengthSquared == 0) return from;
        var proportion = Math.Clamp(((point.X - from.X) * dx + (point.Y - from.Y) * dy) / lengthSquared, 0, 1);
        return new(from.X + dx * proportion, from.Y + dy * proportion);
    }
}
