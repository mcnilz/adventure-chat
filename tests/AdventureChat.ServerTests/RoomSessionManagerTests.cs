using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

[TestFixture]
public class RoomSessionManagerTests
{
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
    public void RequestMove_UnknownConnection_Fails()
    {
        var result = _manager.RequestMove("unknown-conn", new Point2D(500, 300));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("not found"));
    }
}
