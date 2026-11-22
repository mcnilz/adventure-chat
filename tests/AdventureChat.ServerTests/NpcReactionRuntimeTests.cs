using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

public class NpcReactionRuntimeTests
{
    [Test]
    public void ABlockedCallDoesNotResumeAfterThePassageReopensDuringConversation()
    {
        var draft = DynamicNavigationTests.Room();
        draft.Npcs = [new("guard", "Wache", "bruno", new(60, 150), new(80, 150),
            [new("hello", "Hallo", "Guten Tag"), new("bye", "Tschüss", "Auf Wiedersehen")])];
        var switchObject = DynamicNavigationTests.Room().Objects![0];
        switchObject.Id = "switch";
        foreach (var state in switchObject.Interaction!.States) state.Obstacles = null;
        switchObject.Interaction.Actions[0].NpcReaction = new("guard", new(500, 150));
        draft.Objects!.Add(switchObject);
        var rooms = new RoomService();
        rooms.RegisterOrUpdateRoom(draft.ToRoomDefinition());
        var clock = new TestClock();
        var manager = new RoomSessionManager(rooms, new PathfindingService(), clock);
        Assert.That(manager.JoinRoom("operator", "dynamic", "Gast").Success, Is.True);
        Assert.That(manager.JoinRoom("speaker", "dynamic", "Gesprächsgast").Success, Is.True);
        Assert.That(manager.RequestObjectAction("operator", new("dynamic", "door", "open", 0)).Success, Is.True);
        Assert.That(manager.RequestObjectAction("operator", new("dynamic", "switch", "open", 1)).Success, Is.True);

        clock.Now = clock.Now.AddMilliseconds(250);
        var approach = manager.RequestNpcDialogue("speaker", new("dynamic", "guard", ApproachId: "talk", Prepare: true));
        Assert.That(approach.Error, Is.Null);
        var paused = Guard().Position;
        Assert.That(paused.X, Is.GreaterThan(60).And.LessThan(280));
        Assert.That(manager.RequestObjectAction("operator", new("dynamic", "door", "close", 2)).Success, Is.True);
        Assert.That(manager.RequestObjectAction("operator", new("dynamic", "door", "open", 3)).Success, Is.True);
        manager.EndNpcDialogue("speaker", "talk");
        _ = Guard(); // Observe the first unpaused snapshot before checking for movement.
        clock.Now = clock.Now.AddSeconds(5);
        Assert.That(Guard().Position, Is.EqualTo(paused));
        Assert.That(Guard().Motion!.Route, Is.Empty);

        NpcPresence Guard() => manager.GetSnapshot("operator")!.Room!.Npcs!.Single();
    }

    [Test]
    public void ACallInterruptsAPatrolAndTheNpcResumesItsNextWaypoint()
    {
        var draft = DynamicNavigationTests.Room();
        draft.Npcs = [new("guard", "Wache", "bruno", new(60, 150), new(80, 150),
            [new("hello", "Hallo", "Guten Tag")], [new(220, 150), new(60, 150)])];
        draft.Objects![0].Interaction!.Actions[0].NpcReaction = new("guard", new(60, 250));
        var rooms = new RoomService();
        rooms.RegisterOrUpdateRoom(draft.ToRoomDefinition());
        var clock = new TestClock();
        var manager = new RoomSessionManager(rooms, new PathfindingService(), clock);
        Assert.That(manager.JoinRoom("guest", "dynamic", "Gast").Success, Is.True);

        clock.Now = clock.Now.AddMilliseconds(500);
        Assert.That(Guard().Position.X, Is.EqualTo(125).Within(0.001));
        Assert.That(Guard().Position.Y, Is.EqualTo(150));
        Assert.That(manager.RequestObjectAction("guest", new("dynamic", "door", "open", 0)).Success, Is.True);
        Assert.That(Guard().Motion!.Route[^1], Is.EqualTo(new Point2D(60, 250)));
        clock.Now = clock.Now.AddSeconds(1);
        var motion = Guard().Motion!.Route;
        Assert.That(motion[^1], Is.EqualTo(new Point2D(220, 150)));
        Assert.That(Guard().Position.Y, Is.GreaterThan(240));

        NpcPresence Guard() => manager.GetSnapshot("guest")!.Room!.Npcs!.Single();
    }

    [Test]
    public void StaleActionDoesNotCallTheNpcAndARepeatedCallDoesNotRestartTheRoute()
    {
        var world = WorldPackage.Load(Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "../../../../../worlds/reference")));
        var rooms = new RoomService();
        foreach (var room in world.LoadRooms()) rooms.RegisterOrUpdateRoom(room.ToRoomDefinition());
        var clock = new TestClock();
        var manager = new RoomSessionManager(rooms, new PathfindingService(), clock);
        Assert.That(manager.JoinRoom("guest", "lounge", "Gast", new(203, 289)).Success, Is.True);

        Assert.That(manager.RequestObjectAction("guest", new("lounge", "bell", "ring", 1)).Success, Is.False);
        Assert.That(Nela().Position, Is.EqualTo(new Point2D(112, 286)));
        Assert.That(manager.RequestObjectAction("guest", new("lounge", "bell", "ring", 0)).Success, Is.True);
        Assert.That(Nela().Motion!.Route[^1], Is.EqualTo(new Point2D(160, 330)));

        clock.Now = clock.Now.AddMilliseconds(250);
        Assert.That(manager.RequestObjectAction("guest", new("lounge", "bell", "reset", 1)).Success, Is.True);
        var halfway = Nela().Position;
        Assert.That(halfway.X, Is.GreaterThan(112).And.LessThan(160));
        Assert.That(manager.RequestObjectAction("guest", new("lounge", "bell", "ring", 2)).Success, Is.True);
        Assert.That(Nela().Position, Is.EqualTo(halfway));
        clock.Now = clock.Now.AddSeconds(2);
        Assert.That(Nela().Position, Is.EqualTo(new Point2D(160, 330)));
        Assert.That(Nela().Motion!.Route, Is.Empty);

        NpcPresence Nela() => manager.GetSnapshot("guest")!.Room!.Npcs!.Single();
    }

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
