using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

public class ReferenceWorkshopTests
{
    [TestCase("garden", 420, 284, false)]
    [TestCase("garden", 472, 284, false)]
    [TestCase("garden", 472, 306, true)]
    [TestCase("workshop", 49, 90, false)]
    [TestCase("workshop", 49, 118, true)]
    public void WalkingSurface_MatchesTheDoorThreshold(string roomId, int x, int y, bool walkable)
    {
        var room = LoadReference().LoadRooms().Single(room => room.Id == roomId).ToRoomDefinition();
        Assert.That(room.Geometry.IsWalkable(new Point2D(x, y)), Is.EqualTo(walkable));
    }

    [Test]
    public void ReferenceWorkshop_AddsAReachableRoundTripFromTheGarden()
    {
        var rooms = LoadReference().LoadRooms().Select(room => room.ToRoomDefinition()).ToDictionary(room => room.Id);
        Assert.That(rooms.Keys, Does.Contain("workshop"));

        var garden = rooms["garden"];
        var workshop = rooms["workshop"];
        var entrance = garden.Objects!.Single(obj => obj.Id == "workshop-entrance");
        Assert.That(entrance.Size.Height, Is.GreaterThan(garden.ActorHeight),
            "The garden doorway must frame a full-height player figure.");

        var paths = new PathfindingService();
        AssertReachableExit(garden, "from-lounge", "to-workshop", "workshop", "entry", paths);
        AssertReachableExit(workshop, "entry", "to-garden", "garden", "from-workshop", paths);
        AssertReachableExit(garden, "from-workshop", "to-depth-demos", "tour-map", "entry", paths);
    }

    private static WorldPackage LoadReference() => WorldPackage.Load(Path.GetFullPath(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "../../../../../worlds/reference")));

    private static void AssertReachableExit(RoomDefinition room, string start, string exitId,
        string targetRoom, string targetSpawn, PathfindingService paths)
    {
        var spawn = room.Spawns.Single(candidate => candidate.Id == start);
        var exit = room.Exits.Single(candidate => candidate.Id == exitId);
        Assert.That((exit.TargetRoomId, exit.TargetSpawnId), Is.EqualTo((targetRoom, targetSpawn)));
        Assert.That(paths.CalculatePath(room.Geometry, spawn.Position, exit.Position).Success,
            Is.True, $"{room.Id}: {start} → {exitId}");
    }
}
