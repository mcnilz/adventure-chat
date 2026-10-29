using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

[TestFixture]
public class PathfindingTests
{
    private RoomGeometry _geometry = null!;
    private PathfindingService _pathfinder = null!;

    [SetUp]
    public void SetUp()
    {
        var outer = new Polygon([
            new Point2D(40, 240),
            new Point2D(300, 220),
            new Point2D(600, 240),
            new Point2D(600, 345),
            new Point2D(40, 345)
        ]);

        var obstacle = new Polygon([
            new Point2D(250, 255),
            new Point2D(390, 255),
            new Point2D(390, 310),
            new Point2D(250, 310)
        ]);

        _geometry = new RoomGeometry(outer, [obstacle]);
        _pathfinder = new PathfindingService();
    }

    [Test]
    public void DirectLineOfSight_ReturnsDirectPath()
    {
        var start = new Point2D(120, 300);
        var target = new Point2D(200, 300);

        var result = _pathfinder.CalculatePath(_geometry, start, target);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Waypoints, Is.Not.Null);
        Assert.That(result.Waypoints!.Count, Is.EqualTo(2));
        Assert.That(result.Waypoints[0], Is.EqualTo(start));
        Assert.That(result.Waypoints[1], Is.EqualTo(target));
    }

    [Test]
    public void PathBlockedByObstacle_ReturnsDetourAroundObstacle()
    {
        // Start on left side of table, target on right side of table
        var start = new Point2D(120, 300);
        var target = new Point2D(500, 300);

        var result = _pathfinder.CalculatePath(_geometry, start, target);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Waypoints, Is.Not.Null);
        Assert.That(result.Waypoints!.Count, Is.GreaterThan(2), "Path around obstacle must have intermediate waypoints.");

        // First waypoint is start, last waypoint is target
        Assert.That(result.Waypoints.First(), Is.EqualTo(start));
        Assert.That(result.Waypoints.Last(), Is.EqualTo(target));

        // Ensure every intermediate waypoint and segment is strictly outside the obstacle
        foreach (var waypoint in result.Waypoints)
        {
            Assert.That(_geometry.IsWalkable(waypoint), Is.True, $"Waypoint ({waypoint.X}, {waypoint.Y}) must be in walkable area.");
        }
    }

    [Test]
    public void TargetInsideObstacle_IsRejected()
    {
        var start = new Point2D(120, 300);
        var target = new Point2D(320, 280); // inside obstacle

        var result = _pathfinder.CalculatePath(_geometry, start, target);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("not walkable").IgnoreCase);
    }

    [Test]
    public void TargetOutsideOuterBoundary_IsRejected()
    {
        var start = new Point2D(120, 300);
        var target = new Point2D(100, 50); // top wall

        var result = _pathfinder.CalculatePath(_geometry, start, target);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("not walkable").IgnoreCase);
    }

    [Test]
    public void NonFiniteCoordinates_AreRejected()
    {
        var start = new Point2D(120, 300);

        var resultNaN = _pathfinder.CalculatePath(_geometry, start, new Point2D(double.NaN, 300));
        var resultInf = _pathfinder.CalculatePath(_geometry, start, new Point2D(300, double.PositiveInfinity));

        Assert.That(resultNaN.Success, Is.False);
        Assert.That(resultInf.Success, Is.False);
    }

    [Test]
    public void PathAroundObstacle_MaintainsCornerSafetyClearance()
    {
        var start = new Point2D(120, 300);
        var target = new Point2D(500, 300);

        var result = _pathfinder.CalculatePath(_geometry, start, target);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Waypoints, Is.Not.Null);

        // Check each segment for clearance from obstacle corners
        var obstacleVerts = _geometry.Obstacles[0].Vertices;
        for (int i = 0; i < result.Waypoints!.Count - 1; i++)
        {
            var p1 = result.Waypoints[i];
            var p2 = result.Waypoints[i + 1];

            foreach (var corner in obstacleVerts)
            {
                var dist = RoomGeometry.DistanceToSegment(corner, p1, p2);
                Assert.That(dist, Is.GreaterThanOrEqualTo(3.5),
                    $"Path segment ({p1.X},{p1.Y})->({p2.X},{p2.Y}) must not cut closer than 3.5px to corner ({corner.X},{corner.Y}).");
            }
        }
    }

    [Test]
    public void SameInput_ProducesDeterministicResults()
    {
        var start = new Point2D(120, 300);
        var target = new Point2D(500, 270);

        var result1 = _pathfinder.CalculatePath(_geometry, start, target);
        var result2 = _pathfinder.CalculatePath(_geometry, start, target);

        Assert.That(result1.Success, Is.True);
        Assert.That(result2.Success, Is.True);
        Assert.That(result1.Waypoints, Is.EqualTo(result2.Waypoints));
    }
}
