using AdventureChat.Server.Services;
using AdventureChat.Server.Models;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

public class ReferenceWorldTests
{
    [Test]
    public void RedesignedWorld_LoadsAndReachesBothSidesOfTablesAndDoors()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../worlds/reference"));
        var world = WorldPackage.Load(root);
        var rooms = world.LoadRooms().Select(r => r.ToRoomDefinition()).ToArray();
        foreach (var room in rooms.Where(r => r.Id is "lounge" or "garden"))
        {
            Assert.That(room.Objects, Has.Count.EqualTo(1), room.Id);
            var start = room.Spawns.First().Position;
            var behind = new Point2D(330, room.Id == "lounge" ? 258 : 278);
            var front = new Point2D(330, 330);
            var paths = new PathfindingService();
            Assert.That(paths.CalculatePath(room.Geometry, start, behind).Success, Is.True, room.Id + " behind");
            Assert.That(paths.CalculatePath(room.Geometry, behind, front).Success, Is.True, room.Id + " front");
            Assert.That(paths.CalculatePath(room.Geometry, front, room.Exits.Single(e => e.Id != "to-depth-demos").Position).Success, Is.True, room.Id + " door");
            Assert.That(paths.CalculatePath(room.Geometry, start, new(330, 300)).Success, Is.False, room.Id + " solid table");
            Assert.That(paths.CalculatePath(room.Geometry, start, new(330, 230)).Success, Is.EqualTo(room.Id == "lounge"), room.Id + " wall");
        }
    }
}
