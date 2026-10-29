using AdventureChat.Server.Models;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

[TestFixture]
public class GeometryTests
{
    private static RoomGeometry CreateLoungeGeometry()
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

        return new RoomGeometry(outer, [obstacle]);
    }

    [Test]
    public void PointInsideOuterBoundary_AndOutsideObstacle_IsWalkable()
    {
        var geometry = CreateLoungeGeometry();
        var point = new Point2D(120, 300);

        Assert.That(geometry.IsWalkable(point), Is.True);
    }

    [Test]
    public void PointInsideObstacle_IsNotWalkable()
    {
        var geometry = CreateLoungeGeometry();
        var point = new Point2D(320, 280); // Inside obstacle (250..390, 255..310)

        Assert.That(geometry.IsWalkable(point), Is.False);
    }

    [Test]
    public void PointOutsideOuterBoundary_IsNotWalkable()
    {
        var geometry = CreateLoungeGeometry();
        var wallPoint = new Point2D(100, 50); // Outside top wall
        var outOfBounds = new Point2D(-10, 280);

        Assert.That(geometry.IsWalkable(wallPoint), Is.False);
        Assert.That(geometry.IsWalkable(outOfBounds), Is.False);
    }

    [Test]
    public void NonFiniteCoordinates_AreNotWalkable()
    {
        var geometry = CreateLoungeGeometry();

        Assert.That(geometry.IsWalkable(new Point2D(double.NaN, 300)), Is.False);
        Assert.That(geometry.IsWalkable(new Point2D(120, double.PositiveInfinity)), Is.False);
        Assert.That(geometry.IsWalkable(new Point2D(double.NegativeInfinity, double.NaN)), Is.False);
    }
}
