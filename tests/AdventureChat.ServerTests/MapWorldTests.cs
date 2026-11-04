using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

public class MapWorldTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Test]
    public void Map_MovementAndRoomChanges_PreserveAuthorSettings()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../worlds/map"));
        var world = WorldPackage.Load(root);
        var paths = new ContentPathProvider(root, WorldPackage.FindSchema("room.schema.json", world.FormatVersion), root, root);
        var rooms = new RoomService(paths, new RoomYamlSerializer(), new RoomValidator(paths.GetSchemaFilePath()), world);
        var clock = new Clock();
        var sessions = new RoomSessionManager(rooms, new PathfindingService(), clock);
        var first = sessions.JoinRoom("a", "map", "A").Snapshot!;
        Assert.That(first.Room!.ActorHeight, Is.EqualTo(32));
        Assert.That(first.Room.Perspective, Is.EqualTo("top-down"));
        Assert.That(sessions.RequestMove("a", new Point2D(110, 300)).Success, Is.True);
        clock.Now = clock.Now.AddSeconds(0.25);
        var moving = sessions.JoinRoom("b", "map", "B").Snapshot!.Players.Single(p => p.DisplayName == "A");
        Assert.That(moving.Position.X, Is.EqualTo(90).Within(0.01));
        Assert.That(sessions.RequestMove("a", new Point2D(200, 225)).Success, Is.False);
        // Enter the reachable dead-end, turn around and continue through the maze.
        Assert.That(sessions.RequestMove("a", new Point2D(200, 205)).Success, Is.True);
        clock.Now = clock.Now.AddSeconds(10);
        Assert.That(sessions.GetSnapshot("a")!.Players.Single(p => p.DisplayName == "A").Position.Y, Is.EqualTo(205));
        Assert.That(sessions.RequestMove("a", new Point2D(570, 300)).Success, Is.True);
        // An isolated pocket and the hedge itself cannot be reached.
        Assert.That(sessions.RequestMove("a", new Point2D(570, 110)).Success, Is.False);
        Assert.That(sessions.RequestMove("a", new Point2D(140, 150)).Success, Is.False);
        clock.Now = clock.Now.AddSeconds(60);
        var garden = sessions.ChangeRoom("a", "to-garden");
        Assert.That(garden.Success, Is.True);
        Assert.That(garden.Snapshot!.Room!.Perspective, Is.EqualTo("adventure"));
        Assert.That(garden.Snapshot.Room.ActorHeight, Is.EqualTo(64));
        Assert.That(garden.Snapshot.Room.WalkSpeed, Is.EqualTo(130));
        Assert.That(sessions.ChangeRoom("a", "to-map").Success, Is.True);
        Assert.That(sessions.ChangeRoom("a", "to-lounge").Success, Is.True);
        Assert.That(sessions.ChangeRoom("a", "to-map").Success, Is.True);
    }
}
