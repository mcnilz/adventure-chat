using System.Text.Json;
using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

public class DynamicNavigationTests
{
    [Test]
    public void ClosedDoor_BlocksMovementUntilOpened()
    {
        var manager = CreateManager();
        Assert.That(manager.JoinRoom("a", "dynamic", "A").Success, Is.True);
        Assert.That(manager.RequestMove("a", new(500, 150)).Success, Is.False);
        var action = manager.RequestObjectAction("a", new("dynamic", "door", "open", 0));
        Assert.That(action.Success, Is.True, action.Error);
        Assert.That(manager.RequestMove("a", new(500, 150)).Success, Is.True);
    }

    [TestCase(280)]
    [TestCase(300)]
    [TestCase(320)]
    public void ClosingDoor_RejectsFootpointInsideOrOnEitherEdge(double x)
    {
        var manager = CreateManager();
        manager.JoinRoom("a", "dynamic", "A");
        Assert.That(manager.RequestObjectAction("a", new("dynamic", "door", "open", 0)).Success, Is.True);
        manager.JoinRoom("b", "dynamic", "B", new(x, 150));
        var before = manager.GetSnapshot("b")!;
        var result = manager.RequestObjectAction("a", new("dynamic", "door", "close", 1));
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("überdecken"));
        Assert.That(manager.GetSnapshot("b")!.ObjectRevision, Is.EqualTo(before.ObjectRevision));
        Assert.That(manager.GetSnapshot("b")!.ObjectStates, Is.EqualTo(before.ObjectStates));
    }

    [Test]
    public void ClosingDoor_RejectsLossOfLastPreviouslyReachableAnchor()
    {
        var manager = CreateManager();
        manager.JoinRoom("a", "dynamic", "A");
        manager.RequestObjectAction("a", new("dynamic", "door", "open", 0));
        manager.JoinRoom("b", "dynamic", "B", new(500, 150));
        var result = manager.RequestObjectAction("a", new("dynamic", "door", "close", 1));
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("Sicherheitsanker"));
    }

    [Test]
    public void ClosingDoor_StopsAnUnreachableRouteButKeepsCurrentPositionAndAnchor()
    {
        var clock = new TestClock();
        var rooms = new RoomService();
        var room = Room();
        room.Spawns.Add(new() { Id = "right", Position = new() { X = 500, Y = 150 } });
        rooms.RegisterOrUpdateRoom(room.ToRoomDefinition());
        var manager = new RoomSessionManager(rooms, new PathfindingService(), clock);
        manager.JoinRoom("a", "dynamic", "A");
        manager.RequestObjectAction("a", new("dynamic", "door", "open", 0));
        manager.JoinRoom("b", "dynamic", "B", new(500, 150));
        manager.RequestMove("b", new(100, 150));
        clock.Now = clock.Now.AddSeconds(0.5);
        var result = manager.RequestObjectAction("a", new("dynamic", "door", "close", 1));
        Assert.That(result.Success, Is.True, result.Error);
        var player = manager.GetSnapshot("b")!.Players.Single(p => p.DisplayName == "B");
        Assert.That(player.Position.X, Is.EqualTo(435));
        Assert.That(player.State, Is.EqualTo("idle"));
        Assert.That(player.Target, Is.Null);
        Assert.That(result.Changes.Single().StoppedSessions, Does.Contain(player.SessionId));
        Assert.That(manager.RequestMove("b", new(100, 150)).Success, Is.False);
        manager.RequestObjectAction("a", new("dynamic", "door", "open", 2));
        Assert.That(manager.GetSnapshot("b")!.Players.Single(p => p.DisplayName == "B").State, Is.EqualTo("idle"));
        Assert.That(manager.RequestMove("b", new(100, 150)).Success, Is.True);
    }

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [TestCase(279.9999)]
    [TestCase(320.0001)]
    public void ClosingDoor_OutsideEdgeWithRemainingAnchorIsAllowed(double x)
    {
        var room = Room();
        room.Spawns.Add(new() { Id = "right", Position = new() { X = 500, Y = 150 } });
        var rooms = new RoomService(); rooms.RegisterOrUpdateRoom(room.ToRoomDefinition());
        var manager = new RoomSessionManager(rooms, new PathfindingService());
        manager.JoinRoom("a", "dynamic", "A");
        manager.RequestObjectAction("a", new("dynamic", "door", "open", 0));
        manager.JoinRoom("b", "dynamic", "B", new(x, 150));
        var result = manager.RequestObjectAction("a", new("dynamic", "door", "close", 1));
        Assert.That(result.Success, Is.True, result.Error);
    }

    [Test]
    public void ClosingDoor_UsesMovingFootpointAtOneTimeAndDoesNotChangeRejectedRoute()
    {
        var clock = new TestClock();
        var rooms = new RoomService(); rooms.RegisterOrUpdateRoom(Room().ToRoomDefinition());
        var manager = new RoomSessionManager(rooms, new PathfindingService(), clock);
        manager.JoinRoom("a", "dynamic", "A");
        manager.RequestObjectAction("a", new("dynamic", "door", "open", 0));
        manager.JoinRoom("b", "dynamic", "B", new(500, 150));
        manager.RequestMove("b", new(100, 150));
        clock.Now = clock.Now.AddSeconds(1.5); // 500 - 195 = 305, inside the closing door.
        var result = manager.RequestObjectAction("a", new("dynamic", "door", "close", 1));
        Assert.That(result.Success, Is.False);
        var snapshot = manager.GetSnapshot("b")!;
        var player = snapshot.Players.Single(p => p.DisplayName == "B");
        Assert.That(player.Position.X, Is.EqualTo(305));
        Assert.That(player.MovementRevision, Is.EqualTo(1));
        Assert.That(player.Target!.X, Is.EqualTo(100));
        Assert.That(snapshot.ObjectRevision, Is.EqualTo(1));
    }

    [Test]
    public void NewObstacle_ReroutesMovingFigureAroundChair()
    {
        var room = Room();
        var blocker = room.Objects![0].Interaction!.States[0].Obstacles![0];
        blocker.Points = [new() { X = 280, Y = 100 }, new() { X = 320, Y = 100 }, new() { X = 320, Y = 200 }, new() { X = 280, Y = 200 }];
        var rooms = new RoomService(); rooms.RegisterOrUpdateRoom(room.ToRoomDefinition());
        var clock = new TestClock(); var manager = new RoomSessionManager(rooms, new PathfindingService(), clock);
        manager.JoinRoom("a", "dynamic", "A");
        manager.RequestObjectAction("a", new("dynamic", "door", "open", 0));
        manager.JoinRoom("b", "dynamic", "B", new(500, 150));
        manager.RequestMove("b", new(100, 150));
        clock.Now = clock.Now.AddSeconds(0.5);
        var result = manager.RequestObjectAction("a", new("dynamic", "door", "close", 1));
        Assert.That(result.Success, Is.True, result.Error);
        var player = manager.GetSnapshot("b")!.Players.Single(p => p.DisplayName == "B");
        Assert.That(player.State, Is.EqualTo("walk"));
        Assert.That(player.Position.X, Is.EqualTo(435));
        Assert.That(player.Waypoints.Count, Is.GreaterThan(2));
        Assert.That(player.Waypoints.Skip(1).Any(p => p.Y < 100 || p.Y > 200), Is.True);
        Assert.That(player.MovementRevision, Is.EqualTo(2));
        clock.Now = clock.Now.AddSeconds(10);
        Assert.That(manager.GetSnapshot("b")!.Players.Single(p => p.DisplayName == "B").Position.X, Is.EqualTo(100));
    }

    [Test]
    public void LinkedChange_RejectsWholeActionIfFigureInOtherRoomWouldBeCovered()
    {
        var first = Room(); first.Objects![0].Interaction!.InitialState = "open";
        first.Objects[0].Interaction!.Actions.Single(a => a.Id == "close").Effects = [new() { Room = "remote", Object = "door", State = "closed" }];
        var remote = Room(); remote.Id = "remote"; remote.Objects![0].Interaction!.InitialState = "open";
        var rooms = new RoomService(); rooms.RegisterOrUpdateRoom(first.ToRoomDefinition()); rooms.RegisterOrUpdateRoom(remote.ToRoomDefinition());
        var manager = new RoomSessionManager(rooms, new PathfindingService());
        manager.JoinRoom("a", "dynamic", "A"); manager.JoinRoom("b", "remote", "B", new(300, 150));
        var result = manager.RequestObjectAction("a", new("dynamic", "door", "close", 0));
        Assert.That(result.Success, Is.False);
        foreach (var connection in new[] { "a", "b" })
        {
            var snapshot = manager.GetSnapshot(connection)!;
            Assert.That(snapshot.ObjectRevision, Is.Zero);
            Assert.That(snapshot.ObjectStates![0].StateId, Is.EqualTo("open"));
        }
        manager.Leave("b");
        result = manager.RequestObjectAction("a", new("dynamic", "door", "close", 0));
        Assert.That(result.Success, Is.True);
        Assert.That(result.Changes, Has.Count.EqualTo(2));
        Assert.That(result.Changes.Select(c => c.Revision).Distinct().Count(), Is.EqualTo(1));
    }

    [Test]
    public void InitiallyAnchorlessFigure_CannotBeLockedByAnActionEvenIfNewAnchorAppears()
    {
        var room = Room();
        var rooms = new RoomService(); rooms.RegisterOrUpdateRoom(room.ToRoomDefinition());
        var manager = new RoomSessionManager(rooms, new PathfindingService());
        manager.JoinRoom("a", "dynamic", "A");
        manager.JoinRoom("b", "dynamic", "B", new(500, 150));
        var result = manager.RequestObjectAction("a", new("dynamic", "door", "open", 0));
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("Sicherheitsanker"));
        Assert.That(manager.GetSnapshot("a")!.ObjectRevision, Is.Zero);
    }

    [Test]
    public void CaseInsensitiveJoin_StillParticipatesInSafetyChecks()
    {
        var manager = CreateManager(); manager.JoinRoom("a", "dynamic", "A");
        manager.RequestObjectAction("a", new("dynamic", "door", "open", 0));
        Assert.That(manager.JoinRoom("b", "DYNAMIC", "B", new(300, 150)).Success, Is.True);
        Assert.That(manager.RequestObjectAction("a", new("dynamic", "door", "close", 1)).Success, Is.False);
    }

    [Test]
    public void LaterVisualEvent_SubsumesGeometryRouteAndStopFeedbackOfEarlierEvent()
    {
        var room = Room(); room.Spawns.Add(new() { Id = "right", Position = new() { X = 500, Y = 150 } });
        var visual = Room().Objects![0]; visual.Id = "switch";
        foreach (var state in visual.Interaction!.States) state.Obstacles = null;
        room.Objects!.Add(visual);
        var rooms = new RoomService(); rooms.RegisterOrUpdateRoom(room.ToRoomDefinition());
        var clock = new TestClock(); var manager = new RoomSessionManager(rooms, new PathfindingService(), clock);
        manager.JoinRoom("a", "dynamic", "A"); manager.RequestObjectAction("a", new("dynamic", "door", "open", 0));
        manager.JoinRoom("b", "dynamic", "B", new(500, 150)); manager.RequestMove("b", new(100, 150));
        clock.Now = clock.Now.AddSeconds(0.5);
        var closed = manager.RequestObjectAction("a", new("dynamic", "door", "close", 1));
        Assert.That(closed.Success, Is.True);
        var later = manager.RequestObjectAction("a", new("dynamic", "switch", "open", 2));
        var update = later.Changes.Single();
        Assert.That(update.Geometry, Is.Not.Null);
        Assert.That(update.Geometry!.Obstacles, Has.Count.EqualTo(1));
        var stopped = update.Players!.Single(p => p.DisplayName == "B");
        Assert.That(stopped.State, Is.EqualTo("idle"));
        Assert.That(update.StoppedSessions, Does.Contain(stopped.SessionId));
    }

    private static RoomSessionManager CreateManager()
    {
        var rooms = new RoomService();
        rooms.RegisterOrUpdateRoom(Room().ToRoomDefinition());
        return new(rooms, new PathfindingService());
    }

    internal static RoomYamlDto Room() => JsonSerializer.Deserialize<RoomYamlDto>("""
        {"version":1,"id":"dynamic","name":"Dynamic","background":"assets/images/table.png",
         "size":{"width":640,"height":360},"spawns":[{"id":"entry","position":{"x":100,"y":150}}],
         "walkable":[{"id":"floor","points":[{"x":0,"y":0},{"x":640,"y":0},{"x":640,"y":360},{"x":0,"y":360}]}],
         "objects":[{"id":"door","image":"assets/images/table.png","position":{"x":280,"y":100},"size":{"width":40,"height":100},"depthY":200,
           "interaction":{"name":"Door","initialState":"closed","usePoint":{"x":100,"y":150},"useRadius":200,
             "states":[
               {"id":"closed","name":"Closed","image":"assets/images/table.png","position":{"x":280,"y":100},"size":{"width":40,"height":100},"depthY":200,
                "obstacles":[{"id":"door-floor","points":[{"x":280,"y":0},{"x":320,"y":0},{"x":320,"y":360},{"x":280,"y":360}]}]},
               {"id":"open","name":"Open","image":"assets/images/table.png","position":{"x":330,"y":100},"size":{"width":40,"height":100},"depthY":200}],
             "actions":[{"id":"open","name":"Open","from":"closed","to":"open"},{"id":"close","name":"Close","from":"open","to":"closed"}]}}]}
        """, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
}
