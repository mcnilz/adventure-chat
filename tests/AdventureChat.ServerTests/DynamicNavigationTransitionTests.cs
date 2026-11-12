using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

public class DynamicNavigationTransitionTests
{
    [Test]
    public void BlockedExitAndDestinationSpawn_RejectTransitionAndNewEntryWithoutSubstitute()
    {
        var room = DynamicNavigationTests.Room();
        room.Objects![0].Interaction!.InitialState = "open";
        room.Objects[0].Interaction!.States[0].BlockedExits = ["next"];
        room.Exits = [new() { Id = "next", Position = new() { X = 100, Y = 150 }, Radius = 30, Target = new() { Room = "remote", Spawn = "entry" } }];
        var target = DynamicNavigationTests.Room(); target.Id = "remote";
        target.Objects![0].Interaction!.InitialState = "open";
        target.Spawns[0].Position = new() { X = 300, Y = 150 };
        target.Spawns.Add(new() { Id = "safe", Position = new() { X = 100, Y = 150 } });
        room.Objects[0].Interaction!.Actions.Single(a => a.Id == "close").Effects = [new() { Room = "remote", Object = "door", State = "closed" }];
        var rooms = new RoomService(); rooms.RegisterOrUpdateRoom(room.ToRoomDefinition()); rooms.RegisterOrUpdateRoom(target.ToRoomDefinition());
        var manager = new RoomSessionManager(rooms, new PathfindingService());
        manager.JoinRoom("a", "dynamic", "A");
        Assert.That(manager.ChangeRoom("a", "next").Success, Is.True);
        manager.Leave("a"); manager.JoinRoom("a", "dynamic", "A");
        Assert.That(manager.RequestObjectAction("a", new("dynamic", "door", "close", 0)).Success, Is.True);
        Assert.That(manager.GetSnapshot("a")!.Room!.Exits, Is.Empty);
        Assert.That(manager.ChangeRoom("a", "next").Success, Is.False);
        Assert.That(manager.JoinRoom("new", "remote", "New").Success, Is.False);
        Assert.That(manager.GetPlayer("new"), Is.Null);
        manager.RequestObjectAction("a", new("dynamic", "door", "open", 1));
        // Source exit is open again, but the target spawn remains blocked.
        Assert.That(manager.ChangeRoom("a", "next").Success, Is.False);
        Assert.That(manager.GetPlayer("a")!.RoomId, Is.EqualTo("dynamic"));
    }

    [Test]
    public void ShiftedChair_BlocksEachExplicitEndPositionAndReleasesPreviousArea()
    {
        var room = DynamicNavigationTests.Room();
        var states = room.Objects![0].Interaction!.States;
        states[0].Obstacles = [Box(280)];
        states[1].Obstacles = [Box(380)];
        var rooms = new RoomService(); rooms.RegisterOrUpdateRoom(room.ToRoomDefinition());
        var manager = new RoomSessionManager(rooms, new PathfindingService());
        manager.JoinRoom("a", "dynamic", "A");
        Assert.That(manager.RequestMove("a", new(300, 150)).Success, Is.False);
        Assert.That(manager.RequestObjectAction("a", new("dynamic", "door", "open", 0)).Success, Is.True);
        Assert.That(manager.RequestMove("a", new(400, 150)).Success, Is.False);
        Assert.That(manager.RequestMove("a", new(300, 150)).Success, Is.True);
    }

    [Test]
    public void BlockingDestination_RejectsLossOfLastExitAnchorInUnassignedRoom()
    {
        var source = DynamicNavigationTests.Room();
        source.Id = "source"; source.Objects = null;
        source.Spawns[0].Position = new() { X = 100, Y = 150 };
        source.Obstacles = [new() { Id = "divider", Points = [new() { X = 280, Y = 0 }, new() { X = 320, Y = 0 }, new() { X = 320, Y = 359 }, new() { X = 280, Y = 359 }] }];
        // A full-height division; the right-side player can only reach the exit.
        source.Obstacles[0].Points[2].Y = 360; source.Obstacles[0].Points[3].Y = 360;
        source.Exits = [new() { Id = "escape", Position = new() { X = 500, Y = 150 }, Radius = 30, Target = new() { Room = "dynamic", Spawn = "destination" } }];
        var target = DynamicNavigationTests.Room(); target.Objects![0].Interaction!.InitialState = "open";
        target.Spawns.Add(new() { Id = "destination", Position = new() { X = 300, Y = 150 } });
        var rooms = new RoomService(); rooms.RegisterOrUpdateRoom(source.ToRoomDefinition()); rooms.RegisterOrUpdateRoom(target.ToRoomDefinition());
        var manager = new RoomSessionManager(rooms, new PathfindingService());
        manager.JoinRoom("a", "dynamic", "A"); manager.JoinRoom("b", "source", "B", new(500, 150));
        var result = manager.RequestObjectAction("a", new("dynamic", "door", "close", 0));
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("Sicherheitsanker"));
        manager.Leave("b");
        Assert.That(manager.RequestObjectAction("a", new("dynamic", "door", "close", 0)).Success, Is.True);
        manager.JoinRoom("c", "source", "C", new(500, 150));
        Assert.That(manager.GetSnapshot("c")!.Room!.Exits, Is.Empty);
    }

    private static RoomPolygonYamlDto Box(double x) => new() { Id = "chair-floor", Points =
        [new() { X = x, Y = 100 }, new() { X = x + 40, Y = 100 }, new() { X = x + 40, Y = 200 }, new() { X = x, Y = 200 }] };
}
