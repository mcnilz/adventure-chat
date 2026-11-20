using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

public class ObjectTransitionRuntimeTests
{
    [Test]
    public void TargetGeometryChangesImmediately_AndReverseWaitsForTheTransition()
    {
        var room = DynamicNavigationTests.Room();
        room.Objects![0].Interaction!.Transitions =
        [
            new() { From = "closed", To = "open", DurationMs = 900 },
            new() { From = "open", To = "closed", DurationMs = 650 }
        ];
        var rooms = new RoomService();
        rooms.RegisterOrUpdateRoom(room.ToRoomDefinition());
        var clock = new TestClock();
        var manager = new RoomSessionManager(rooms, new PathfindingService(), clock);
        manager.JoinRoom("a", "dynamic", "A");
        Assert.That(manager.RequestObjectAction("a", new("dynamic", "door", "open", 0)).Success, Is.True);
        Assert.That(manager.RequestMove("a", new(500, 150)).Success, Is.True);

        clock.Now = clock.Now.AddMilliseconds(400);
        manager.JoinRoom("b", "dynamic", "B");
        var during = manager.GetSnapshot("b")!;
        Assert.That(during.ObjectStates!.Single().StateId, Is.EqualTo("open"));
        Assert.That(during.ObjectStates!.Single().Transition?.ElapsedMs, Is.EqualTo(400));
        Assert.That(during.ObjectStates!.Single().Transition?.DurationMs, Is.EqualTo(900));
        var blocked = manager.RequestObjectAction("b", new("dynamic", "door", "close", during.ObjectRevision));
        Assert.That(blocked.Success, Is.False);
        Assert.That(blocked.Error, Does.Contain("bewegt"));
        Assert.That(manager.GetSnapshot("b")!.ObjectRevision, Is.EqualTo(during.ObjectRevision));

        clock.Now = clock.Now.AddMilliseconds(500);
        Assert.That(manager.GetSnapshot("b")!.ObjectStates!.Single().Transition, Is.Null);
        Assert.That(manager.RequestObjectAction("b", new("dynamic", "door", "close", during.ObjectRevision)).Success, Is.True);
        Assert.That(manager.RequestMove("b", new(500, 150)).Success, Is.False);
        Assert.That(manager.GetSnapshot("b")!.ObjectStates!.Single().Transition?.DurationMs, Is.EqualTo(650));
    }

    [Test]
    public void UnsafeTargetDoesNotStartATransition()
    {
        var room = DynamicNavigationTests.Room();
        var door = room.Objects![0].Interaction!;
        door.InitialState = "open";
        door.Transitions = [new() { From = "open", To = "closed", DurationMs = 600 }];
        var rooms = new RoomService();
        rooms.RegisterOrUpdateRoom(room.ToRoomDefinition());
        var manager = new RoomSessionManager(rooms, new PathfindingService());
        manager.JoinRoom("a", "dynamic", "A");
        manager.JoinRoom("b", "dynamic", "B", new(300, 150));
        var refused = manager.RequestObjectAction("a", new("dynamic", "door", "close", 0));
        Assert.That(refused.Error, Does.Contain("überdecken"));
        Assert.That(refused.Changes, Is.Empty);
        Assert.That(manager.GetSnapshot("b")!.ObjectStates!.Single(), Is.EqualTo(new ObjectStateDto("door", "open")));
    }

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
