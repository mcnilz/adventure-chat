using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

public class ObjectActorAnimationRuntimeTests
{
    [Test]
    public void HoldRequiresTheSelectedPointAndAnAvailablePlace()
    {
        var room = DynamicNavigationTests.Room();
        var obj = room.Objects![0];
        obj.UsePoints = [new() { X = 100, Y = 150 }, new() { X = 130, Y = 150 }];
        obj.Interaction!.Actions.Add(new()
        {
            Id = "pose", Name = "Benutzen", From = "closed", To = "closed",
            ActorAnimation = new() { Point = 1, Direction = "left" }
        });
        var rooms = new RoomService();
        rooms.RegisterOrUpdateRoom(room.ToRoomDefinition());
        var manager = new RoomSessionManager(rooms, new PathfindingService());
        manager.JoinRoom("a", "dynamic", "A");
        manager.JoinRoom("b", "dynamic", "B", new(130, 150));
        manager.JoinRoom("c", "dynamic", "C", new(130, 150));
        ObjectActionResult Use(string connection) => manager.RequestObjectAction(connection,
            new("dynamic", "door", "pose", manager.GetSnapshot(connection)!.ObjectRevision));

        Assert.That(Use("a").ErrorCode, Is.EqualTo("too_far"));
        var started = Use("b");
        Assert.That(started.Success, Is.True, started.Error);
        var snapshot = manager.GetSnapshot("c")!;
        var actor = snapshot.Players.Single(player => player.DisplayName == "B");
        Assert.Multiple(() =>
        {
            Assert.That(actor.Direction, Is.EqualTo("left"));
            Assert.That(actor.ActorAnimation, Is.Not.Null);
            Assert.That(actor.ActorAnimation!.Image, Is.Null);
            Assert.That(Use("b").Changes, Is.Empty);
            Assert.That(Use("c").Error, Does.Contain("bereits belegt"));
        });

        manager.Leave("b");
        Assert.That(Use("c").Success, Is.True);
        var blockedWalk = manager.RequestMove("c", new(500, 150));
        Assert.That(blockedWalk.Success, Is.False);
        Assert.That(manager.GetSnapshot("c")!.Players.Single(player => player.DisplayName == "C").ActorAnimation, Is.Null);
    }

    [Test]
    public void AStateObstacleCanBlockTheAnimationPointEvenWhenThePlayerIsNear()
    {
        var room = DynamicNavigationTests.Room();
        var obj = room.Objects![0];
        obj.UsePoints = [new() { X = 300, Y = 150 }];
        obj.Interaction!.Actions[0].ActorAnimation = new() { Point = 0, Direction = "up", DurationMs = 500 };
        var rooms = new RoomService();
        rooms.RegisterOrUpdateRoom(room.ToRoomDefinition());
        var manager = new RoomSessionManager(rooms, new PathfindingService());
        manager.JoinRoom("a", "dynamic", "A");
        var result = manager.RequestObjectAction("a", new("dynamic", "door", "open", 0));
        Assert.That(result.Error, Does.Contain("Bedienpunkt ist derzeit gesperrt"));
        Assert.That(manager.GetSnapshot("a")!.Players.Single().ActorAnimation, Is.Null);
    }

    [Test]
    public void TheFirstServerArrivalWinsEvenWhenBothArrivalsBecomeDueTogether()
    {
        var room = DynamicNavigationTests.Room();
        room.Objects![0].Interaction!.Actions.Add(new()
        {
            Id = "pose", Name = "Benutzen", From = "closed", To = "closed",
            ActorAnimation = new() { Point = 0, Direction = "left" }
        });
        var rooms = new RoomService();
        rooms.RegisterOrUpdateRoom(room.ToRoomDefinition());
        var clock = new TestClock();
        var manager = new RoomSessionManager(rooms, new PathfindingService(), clock);
        manager.JoinRoom("a", "dynamic", "A", new(220, 150));
        manager.JoinRoom("b", "dynamic", "B", new(130, 150));
        var command = new ObjectActionRequestDto("dynamic", "door", "pose", 0, Approach: true);

        Assert.That(manager.RequestObjectAction("a", command).Move?.Success, Is.True);
        clock.Now = clock.Now.AddMilliseconds(100);
        Assert.That(manager.RequestObjectAction("b", command).Move?.Success, Is.True);
        Assert.That(manager.RequestObjectAction("b", command).Move, Is.Null, "The repeated command preserves the route.");
        clock.Now = clock.Now.AddSeconds(1);
        var players = manager.GetSnapshot("a")!.Players;
        Assert.That(players.Single(player => player.ActorAnimation is not null).DisplayName, Is.EqualTo("B"));
    }

    [Test]
    public void TheSameActionIdInANewObjectStateCanReplaceAnEarlierApproach()
    {
        var room = DynamicNavigationTests.Room();
        var obj = room.Objects![0];
        obj.UsePoints = [new() { X = 100, Y = 150 }, new() { X = 130, Y = 150 }];
        obj.Interaction!.Actions.AddRange([
            new() { Id = "use", Name = "Benutzen", From = "closed", To = "closed", ActorAnimation = new() { Point = 0, Direction = "left" } },
            new() { Id = "use", Name = "Benutzen", From = "open", To = "open", ActorAnimation = new() { Point = 1, Direction = "right" } }
        ]);
        var rooms = new RoomService();
        rooms.RegisterOrUpdateRoom(room.ToRoomDefinition());
        var clock = new TestClock();
        var manager = new RoomSessionManager(rooms, new PathfindingService(), clock);
        manager.JoinRoom("a", "dynamic", "A", new(220, 150));
        manager.JoinRoom("b", "dynamic", "B");
        var command = new ObjectActionRequestDto("dynamic", "door", "use", 0, Approach: true);

        Assert.That(manager.RequestObjectAction("a", command).Move?.Target, Is.EqualTo(new PointDto(100, 150)));
        Assert.That(manager.RequestObjectAction("b", new("dynamic", "door", "open", 0)).Success, Is.True);
        Assert.That(manager.RequestObjectAction("a", command with { ExpectedRevision = 1 }).Move?.Target, Is.EqualTo(new PointDto(130, 150)));
        clock.Now = clock.Now.AddSeconds(1);
        var actor = manager.GetSnapshot("a")!.Players.Single(player => player.DisplayName == "A");
        Assert.That(actor.ActorAnimation, Is.Not.Null);
        Assert.That(actor.Direction, Is.EqualTo("right"));
    }

    [TestCase(499, false)]
    [TestCase(500, true)]
    public void AWalkCancelsTheGestureOnlyBeforeItsDeadline(int elapsed, bool effectApplied)
    {
        var room = DynamicNavigationTests.Room();
        room.Objects![0].Interaction!.Actions[0].ActorAnimation = new() { Point = 0, Direction = "up", DurationMs = 500 };
        var rooms = new RoomService();
        rooms.RegisterOrUpdateRoom(room.ToRoomDefinition());
        var clock = new TestClock();
        var manager = new RoomSessionManager(rooms, new PathfindingService(), clock);
        manager.JoinRoom("a", "dynamic", "A");
        Assert.That(manager.RequestObjectAction("a", new("dynamic", "door", "open", 0)).Pending, Is.True);
        clock.Now = clock.Now.AddMilliseconds(elapsed);

        Assert.That(manager.RequestMove("a", new(500, 150)).Success, Is.EqualTo(effectApplied));
        clock.Now = clock.Now.AddSeconds(1);
        Assert.That(manager.GetSnapshot("a")!.ObjectStates!.Single().StateId, Is.EqualTo(effectApplied ? "open" : "closed"));
    }

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
