using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

[TestFixture]
public class RoomSessionManagerTests
{
    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Test]
    public void JoinRoom_DuringMovement_SnapshotContainsCurrentPositionAndRemainingRoute()
    {
        var clock = new TestClock();
        var manager = new RoomSessionManager(new RoomService(), new PathfindingService(), clock);
        manager.JoinRoom("a", "lounge", "A");
        Assert.That(manager.RequestMove("a", new Point2D(220, 300)).Success, Is.True);
        clock.Now = clock.Now.AddSeconds(0.5);

        var player = manager.JoinRoom("b", "lounge", "B").Snapshot!.Players.Single(p => p.DisplayName == "A");
        Assert.That(player.Position.X, Is.EqualTo(185).Within(0.01));
        Assert.That(player.Waypoints[0].X, Is.EqualTo(185).Within(0.01));
        Assert.That(player.Waypoints[^1].X, Is.EqualTo(220));
        clock.Now = clock.Now.AddSeconds(1);
        player = manager.JoinRoom("c", "lounge", "C").Snapshot!.Players.Single(p => p.DisplayName == "A");
        Assert.That(player.Position.X, Is.EqualTo(220));
        Assert.That(player.State, Is.EqualTo("idle"));
        Assert.That(player.Waypoints, Is.Empty);
    }

    [Test]
    public void RequestMove_DuringMovement_StartsAtCurrentServerPosition()
    {
        var clock = new TestClock();
        var manager = new RoomSessionManager(new RoomService(), new PathfindingService(), clock);
        manager.JoinRoom("a", "lounge", "A");
        manager.RequestMove("a", new Point2D(220, 300));
        clock.Now = clock.Now.AddSeconds(0.5);
        var result = manager.RequestMove("a", new Point2D(200, 320), new Point2D(100, 300));
        Assert.That(result.Success, Is.True);
        Assert.That(result.Waypoints![0].X, Is.EqualTo(185).Within(0.01));
    }

    [Test]
    public void RequestMove_InvalidTarget_DoesNotCancelExistingMovement()
    {
        var clock = new TestClock();
        var manager = new RoomSessionManager(new RoomService(), new PathfindingService(), clock);
        manager.JoinRoom("a", "lounge", "A");
        manager.RequestMove("a", new Point2D(220, 300));
        clock.Now = clock.Now.AddSeconds(0.5);
        Assert.That(manager.RequestMove("a", new Point2D(320, 280)).Success, Is.False);
        clock.Now = clock.Now.AddSeconds(1);
        var player = manager.JoinRoom("b", "lounge", "B").Snapshot!.Players.Single(p => p.DisplayName == "A");
        Assert.That(player.State, Is.EqualTo("idle"));
        Assert.That(player.Position.X, Is.EqualTo(220));
    }

    [Test]
    public void UpdatePlayerProgress_CannotTeleportPlayer()
    {
        _manager.JoinRoom("a", "lounge", "A");
        _manager.UpdatePlayerProgress("a", new Point2D(200, 300), "left", "idle");
        Assert.That(_manager.GetPlayer("a")!.Position, Is.EqualTo(new Point2D(120, 300)));
    }
    private IRoomService _roomService = null!;
    private IPathfindingService _pathfinder = null!;
    private IRoomSessionManager _manager = null!;

    [SetUp]
    public void Setup()
    {
        _roomService = new RoomService();
        _pathfinder = new PathfindingService();
        _manager = new RoomSessionManager(_roomService, _pathfinder);
    }

    [Test]
    public void JoinRoom_ValidParameters_ReturnsSuccessWithSnapshot()
    {
        var result = _manager.JoinRoom("conn-1", "lounge", "Guybrush");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Session, Is.Not.Null);
        Assert.That(result.Session!.DisplayName, Is.EqualTo("Guybrush"));
        Assert.That(result.Session.RoomId, Is.EqualTo("lounge"));
        Assert.That(result.Snapshot, Is.Not.Null);
        Assert.That(result.Snapshot!.RoomId, Is.EqualTo("lounge"));
        Assert.That(result.Snapshot.Players.Count, Is.EqualTo(1));
        Assert.That(result.Snapshot.YourSessionId, Is.EqualTo(result.Session.SessionId));
    }

    [Test]
    public void JoinRoom_EmptyName_GeneratesDefaultGuestName()
    {
        var result = _manager.JoinRoom("conn-2", "lounge", "   ");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Session!.DisplayName, Does.StartWith("Gast-"));
    }

    [Test]
    public void JoinRoom_LongName_TruncatesTo24Characters()
    {
        var longName = "A Very Long Display Name Exceeding Twenty Four Chars";
        var result = _manager.JoinRoom("conn-3", "lounge", longName);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Session!.DisplayName.Length, Is.LessThanOrEqualTo(24));
    }

    [Test]
    public void JoinRoom_UnknownRoom_ReturnsError()
    {
        var result = _manager.JoinRoom("conn-4", "nonexistent-room", "Guybrush");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("not found"));
    }

    [Test]
    public void JoinRoom_MultiplePlayers_SnapshotContainsAllCurrentPlayers()
    {
        _manager.JoinRoom("conn-1", "lounge", "Guybrush");
        var result2 = _manager.JoinRoom("conn-2", "lounge", "Elaine");

        Assert.That(result2.Success, Is.True);
        Assert.That(result2.Snapshot!.Players.Count, Is.EqualTo(2));
        Assert.That(result2.Snapshot.Players.Select(p => p.DisplayName), Does.Contain("Guybrush").And.Contain("Elaine"));
    }

    [Test]
    public void JoinRoom_AlreadyJoinedConnection_IsRejectedWithoutMovingSession()
    {
        var first = _manager.JoinRoom("conn-1", "lounge", "A");
        var second = _manager.JoinRoom("conn-1", "garden", "A");
        Assert.That(second.Success, Is.False);
        Assert.That(_manager.GetPlayer("conn-1")!.SessionId, Is.EqualTo(first.Session!.SessionId));
        Assert.That(_manager.GetPlayer("conn-1")!.RoomId, Is.EqualTo("lounge"));
    }

    [Test]
    public void Leave_ExistingPlayer_RemovesFromRoom()
    {
        _manager.JoinRoom("conn-1", "lounge", "Guybrush");
        var left = _manager.Leave("conn-1");

        Assert.That(left, Is.Not.Null);
        Assert.That(left!.DisplayName, Is.EqualTo("Guybrush"));
        Assert.That(_manager.GetPlayersInRoom("lounge").Count, Is.EqualTo(0));
    }

    [Test]
    public void Leave_NonExistingConnection_ReturnsNull()
    {
        var left = _manager.Leave("unknown-conn");
        Assert.That(left, Is.Null);
    }

    [Test]
    public void RequestMove_ValidTarget_CalculatesPathAndUpdatesSession()
    {
        _manager.JoinRoom("conn-1", "lounge", "Guybrush");
        // Move towards right side of the lounge (e.g. 500, 300)
        var result = _manager.RequestMove("conn-1", new Point2D(500, 300));

        Assert.That(result.Success, Is.True);
        Assert.That(result.Target, Is.Not.Null);
        Assert.That(result.Target!.X, Is.EqualTo(500));
        Assert.That(result.Target.Y, Is.EqualTo(300));
        Assert.That(result.Waypoints, Is.Not.Null);
        Assert.That(result.Waypoints!.Count, Is.GreaterThan(0));

        var player = _manager.GetPlayer("conn-1");
        Assert.That(player!.State, Is.EqualTo("walk"));
    }

    [Test]
    public void RequestMove_TargetInsideObstacle_Fails()
    {
        _manager.JoinRoom("conn-1", "lounge", "Guybrush");
        // Center table obstacle is ~ 320, 280
        var result = _manager.RequestMove("conn-1", new Point2D(320, 280));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("walkable").Or.Contain("obstacle"));
    }

    [Test]
    public void RequestMove_ClientPositionCannotOverrideServerPosition()
    {
        var join = _manager.JoinRoom("conn-1", "lounge", "Guybrush");
        var serverPosition = join.Session!.Position;
        // Intermediate position while walking
        var intermediatePos = new Point2D(200, 300);
        var target = new Point2D(500, 300);

        var result = _manager.RequestMove("conn-1", target, intermediatePos);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Waypoints, Is.Not.Null);
        var waypoints = result.Waypoints!;
        Assert.That(waypoints[0].X, Is.EqualTo(serverPosition.X));
        Assert.That(waypoints[0].Y, Is.EqualTo(serverPosition.Y));

        var player = _manager.GetPlayer("conn-1");
        Assert.That(player!.Position, Is.EqualTo(serverPosition));
    }

    [Test]
    public void RequestMove_UnknownConnection_Fails()
    {
        var result = _manager.RequestMove("unknown-conn", new Point2D(500, 300));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("not found"));
    }

    [Test]
    public void SendChatMessage_ValidMessage_ReturnsSuccessAndStoresInRoomHistory()
    {
        _manager.JoinRoom("conn-1", "lounge", "Guybrush");

        var result = _manager.SendChatMessage("conn-1", "Hello SCUMM Bar!");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Is.Not.Null);
        Assert.That(result.Message!.Text, Is.EqualTo("Hello SCUMM Bar!"));
        Assert.That(result.Message.SenderName, Is.EqualTo("Guybrush"));
        Assert.That(result.Message.RoomId, Is.EqualTo("lounge"));
        Assert.That(result.Message.MessageId, Is.Not.Null.And.Not.Empty);

        var history = _manager.GetRecentMessages("lounge");
        Assert.That(history.Count, Is.EqualTo(1));
        Assert.That(history[0].Text, Is.EqualTo("Hello SCUMM Bar!"));
    }

    [Test]
    public void SendChatMessage_EmptyOrWhitespaceMessage_Fails()
    {
        _manager.JoinRoom("conn-1", "lounge", "Guybrush");

        var result1 = _manager.SendChatMessage("conn-1", "");
        var result2 = _manager.SendChatMessage("conn-1", "   \t \n ");

        Assert.That(result1.Success, Is.False);
        Assert.That(result1.Error, Does.Contain("empty"));
        Assert.That(result2.Success, Is.False);
        Assert.That(result2.Error, Does.Contain("empty"));
    }

    [Test]
    public void SendChatMessage_MessageExceeding200Chars_Fails()
    {
        _manager.JoinRoom("conn-1", "lounge", "Guybrush");
        var longMessage = new string('x', 201);

        var result = _manager.SendChatMessage("conn-1", longMessage);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("200"));
    }

    [Test]
    public void SendChatMessage_ExceedingRateLimit_RejectsExcessMessages()
    {
        _manager.JoinRoom("conn-1", "lounge", "Guybrush");

        // Send 5 allowed messages quickly
        for (int i = 1; i <= 5; i++)
        {
            var res = _manager.SendChatMessage("conn-1", $"Msg {i}");
            Assert.That(res.Success, Is.True, $"Message {i} should succeed within rate limit");
        }

        // 6th message should be rejected due to rate limiting
        var blockedRes = _manager.SendChatMessage("conn-1", "Msg 6 (spammed)");
        Assert.That(blockedRes.Success, Is.False);
        Assert.That(blockedRes.Error, Does.Contain("Rate limit"));
    }

    [Test]
    public void SendChatMessage_MaintainsMax50MessagesInRoomHistory()
    {
        _manager.JoinRoom("conn-1", "lounge", "Guybrush");

        // Send 60 messages from different mock connections or within limit simulation
        for (int i = 1; i <= 60; i++)
        {
            var conn = $"conn-{i}";
            _manager.JoinRoom(conn, "lounge", $"Player-{i}");
            var res = _manager.SendChatMessage(conn, $"Message #{i}");
            Assert.That(res.Success, Is.True);
        }

        var history = _manager.GetRecentMessages("lounge");
        Assert.That(history.Count, Is.EqualTo(50));
        // Oldest messages 1..10 should have been pruned; oldest remaining is #11, newest is #60
        Assert.That(history[0].Text, Is.EqualTo("Message #11"));
        Assert.That(history[^1].Text, Is.EqualTo("Message #60"));
    }

    [Test]
    public void JoinRoom_ProvidesRecentRoomMessagesInSnapshot()
    {
        _manager.JoinRoom("conn-1", "lounge", "Guybrush");
        _manager.SendChatMessage("conn-1", "Welcome to the tavern!");

        var joinResult = _manager.JoinRoom("conn-2", "lounge", "Elaine");

        Assert.That(joinResult.Success, Is.True);
        Assert.That(joinResult.Snapshot, Is.Not.Null);
        Assert.That(joinResult.Snapshot!.RecentMessages.Count, Is.EqualTo(1));
        Assert.That(joinResult.Snapshot.RecentMessages[0].Text, Is.EqualTo("Welcome to the tavern!"));
        Assert.That(joinResult.Snapshot.RecentMessages[0].SenderName, Is.EqualTo("Guybrush"));
    }

    [Test]
    public void SendChatMessage_UnknownConnection_Fails()
    {
        var result = _manager.SendChatMessage("unknown-conn", "Hello?");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("not found"));
    }

    [Test]
    public void ChangeRoom_ValidTarget_MovesPlayerToTargetRoomAndReturnsSnapshot()
    {
        _manager.JoinRoom("conn-1", "lounge", "Guybrush", new Point2D(540, 300));

        var result = _manager.ChangeRoom("conn-1", "to-garden");

        Assert.That(result.Success, Is.True);
        Assert.That(result.OldRoomId, Is.EqualTo("lounge"));
        Assert.That(result.Session, Is.Not.Null);
        Assert.That(result.Session!.RoomId, Is.EqualTo("garden"));
        Assert.That(result.Snapshot, Is.Not.Null);
        Assert.That(result.Snapshot!.RoomId, Is.EqualTo("garden"));
        Assert.That(result.Snapshot.Room, Is.Not.Null);
        Assert.That(result.Snapshot.Room!.Name, Is.EqualTo("Tavernen-Garten"));

        var loungePlayers = _manager.GetPlayersInRoom("lounge");
        var gardenPlayers = _manager.GetPlayersInRoom("garden");

        Assert.That(loungePlayers.Count, Is.EqualTo(0));
        Assert.That(gardenPlayers.Count, Is.EqualTo(1));
        Assert.That(gardenPlayers[0].DisplayName, Is.EqualTo("Guybrush"));
    }

    [Test]
    public void ChangeRoom_WithTargetSpawn_PositionsPlayerAtSpecifiedSpawn()
    {
        _manager.JoinRoom("conn-1", "lounge", "Guybrush", new Point2D(540, 300));

        var result = _manager.ChangeRoom("conn-1", "to-garden");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Session!.Position, Is.EqualTo(new Point2D(80, 300)));
    }

    [Test]
    public void ChangeRoom_TargetRoomNotFound_FailsAndKeepsPlayerInOldRoom()
    {
        _manager.JoinRoom("conn-1", "lounge", "Guybrush");

        var result = _manager.ChangeRoom("conn-1", "non-existent-room");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("not found"));

        var player = _manager.GetPlayer("conn-1");
        Assert.That(player!.RoomId, Is.EqualTo("lounge"));
    }

    [Test]
    public void ChangeRoom_UnknownConnection_Fails()
    {
        var result = _manager.ChangeRoom("unknown-conn", "garden");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("not found"));
    }

    [Test]
    public void ChangeRoom_ExitTooFarAway_LeavesSessionInCurrentRoom()
    {
        _manager.JoinRoom("conn-1", "lounge", "A");
        var result = _manager.ChangeRoom("conn-1", "garden");
        Assert.That(result.Success, Is.False);
        var distantExit = _manager.ChangeRoom("conn-1", "to-garden");
        Assert.That(distantExit.Success, Is.False);
        Assert.That(distantExit.Error, Does.Contain("far"));
        Assert.That(_manager.GetPlayer("conn-1")!.RoomId, Is.EqualTo("lounge"));
    }

    [Test]
    public void ChangeRoom_ValidNearbyExit_UsesServerDestination()
    {
        _manager.JoinRoom("conn-1", "lounge", "A", new Point2D(540, 300));
        var result = _manager.ChangeRoom("conn-1", "to-garden");
        Assert.That(result.Success, Is.True);
        Assert.That(result.Session!.RoomId, Is.EqualTo("garden"));
        Assert.That(result.Session.Position, Is.EqualTo(new Point2D(80, 300)));
    }

    [Test]
    public void ChangeRoom_IsolatesChatMessagesBetweenRooms()
    {
        _manager.JoinRoom("conn-1", "lounge", "Guybrush");
        _manager.JoinRoom("conn-2", "lounge", "Elaine", new Point2D(540, 300));

        // Elaine moves to garden
        _manager.ChangeRoom("conn-2", "to-garden");

        // Guybrush chats in lounge
        var loungeMsg = _manager.SendChatMessage("conn-1", "Hello Lounge!");
        Assert.That(loungeMsg.Success, Is.True);

        // Elaine chats in garden
        var gardenMsg = _manager.SendChatMessage("conn-2", "Hello Garden!");
        Assert.That(gardenMsg.Success, Is.True);

        var loungeHistory = _manager.GetRecentMessages("lounge");
        var gardenHistory = _manager.GetRecentMessages("garden");

        Assert.That(loungeHistory.Count, Is.EqualTo(1));
        Assert.That(loungeHistory[0].Text, Is.EqualTo("Hello Lounge!"));

        Assert.That(gardenHistory.Count, Is.EqualTo(1));
        Assert.That(gardenHistory[0].Text, Is.EqualTo("Hello Garden!"));
    }
}
