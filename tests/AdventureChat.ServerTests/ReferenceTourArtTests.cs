using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

public class ReferenceTourArtTests
{
    private static WorldPackage LoadWorld() => WorldPackage.Load(Path.GetFullPath(
        Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../worlds/reference")));

    [Test]
    public void ReferenceArt_UsesExistingFormatAndNewNavigationContentVersion()
    {
        var world = LoadWorld();
        Assert.That(world.FormatVersion, Is.EqualTo("1.5.0"));
        Assert.That(world.ContentVersion, Is.EqualTo("3.0.0"));
        Assert.That(world.Characters.Select(c => c.Id), Is.EquivalentTo(new[] { "mara", "bruno" }));
    }

    private static IEnumerable<TestCaseData> TourRoutes()
    {
        yield return new TestCaseData("tour-map",
            new Point2D[] { new(140, 310), new(330, 220), new(650, 90), new(940, 175), new(1220, 180) },
            new Point2D[] { new(630, 250), new(1155, 280), new(200, 220), new(100, 325) });
        yield return new TestCaseData("tour-depth",
            new Point2D[] { new(100, 300), new(320, 140), new(320, 240), new(570, 285) },
            new Point2D[] { new(100, 180), new(500, 180) });
        yield return new TestCaseData("tour-combined",
            new Point2D[] { new(170, 290), new(225, 140), new(600, 325), new(990, 140), new(1130, 300) },
            new Point2D[] { new(600, 200), new(400, 180), new(850, 160) });
    }

    [TestCaseSource(nameof(TourRoutes))]
    public void TourRoutes_ConnectVisiblePathsAndExcludeScenery(
        string id, Point2D[] waypoints, Point2D[] scenery)
    {
        var room = LoadWorld().LoadRooms().Single(r => r.Id == id).ToRoomDefinition();
        var paths = new PathfindingService();
        var previous = room.SpawnPoint;
        foreach (var point in waypoints)
        {
            Assert.That(paths.CalculatePath(room.Geometry, previous, point).Success, Is.True,
                $"{id}: visible path {previous} -> {point}");
            previous = point;
        }
        foreach (var exit in room.Exits)
            Assert.That(paths.CalculatePath(room.Geometry, previous, exit.Position).Success, Is.True,
                $"{id}: exit {exit.Id}");
        foreach (var point in scenery)
            Assert.That(paths.CalculatePath(room.Geometry, room.SpawnPoint, point).Success, Is.False,
                $"{id}: scenery must not be walkable at {point}");
    }
}
