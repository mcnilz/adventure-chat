namespace AdventureChat.Server.Models;

public sealed record PathResult(bool Success, string? Error, IReadOnlyList<Point2D>? Waypoints)
{
    public static PathResult Ok(IReadOnlyList<Point2D> waypoints) => new(true, null, waypoints);
    public static PathResult Fail(string error) => new(false, error, null);
}
