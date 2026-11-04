using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace AdventureChat.Server.Hubs;

public class RoomHub : Hub
{
    private readonly IRoomSessionManager _legacySessions;
    private readonly WorldRuntimeCatalog _worlds;
    private WorldRuntimeCatalog.Runtime? CurrentWorld => Context.Items.TryGetValue("world", out var value) ? value as WorldRuntimeCatalog.Runtime : null;
    private IRoomSessionManager Sessions => CurrentWorld?.Sessions ?? _legacySessions;
    private string Group(string roomId) => CurrentWorld is { } world ? $"world:{world.Package.Id}:room:{roomId.ToLowerInvariant()}" : roomId;
    private readonly WorldPackage? _world;

    public RoomHub(IRoomSessionManager sessionManager, WorldRuntimeCatalog worlds, WorldPackage? world = null)
    {
        _legacySessions = sessionManager;
        _worlds = worlds;
        _world = world;
    }

    public async Task<JoinRoomResultDto> JoinRoom(JoinRoomRequestDto request)
    {
        if (Sessions.GetPlayer(Context.ConnectionId) is not null)
            return new JoinRoomResultDto(false, "Connection has already joined a room.", null);
        var selected = request.WorldId is null ? (_world is null ? null : _worlds.Find(_world.Id)) : _worlds.Find(request.WorldId);
        if (request.WorldId is not null && selected is null)
            return new JoinRoomResultDto(false, "Unknown world.", null);
        if (selected is not null && !string.Equals(request.RoomId, selected.Package.StartRoom, StringComparison.OrdinalIgnoreCase))
            return new JoinRoomResultDto(false, "Join the world at its start room.", null);
        if (selected is not null) Context.Items["world"] = selected;
        var result = Sessions.JoinRoom(Context.ConnectionId, request.RoomId, request.DisplayName);
        if (!result.Success || result.Session is null || result.Snapshot is null)
        {
            return new JoinRoomResultDto(false, result.Error ?? "Could not join room.", null);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, Group(request.RoomId));
        var snapshot = Sessions.GetSnapshot(Context.ConnectionId, includeHistory: true);
        if (snapshot is null) return new JoinRoomResultDto(false, "Session not found.", null);

        // Notify other players in the room about the new participant
        await Clients.OthersInGroup(Group(request.RoomId)).SendAsync("UserJoined", new UserJoinedDto(result.Session.ToDto(), request.RoomId));

        return new JoinRoomResultDto(true, null, snapshot);
    }

    public async Task<ChangeRoomResultDto> ChangeRoom(ChangeRoomRequestDto request)
    {
        var result = Sessions.ChangeRoom(Context.ConnectionId, request.ExitId);
        if (!result.Success || result.Session is null || result.Snapshot is null)
        {
            return new ChangeRoomResultDto(false, result.Error ?? "Could not change room.", null);
        }

        // 1. Leave old room group & notify old room participants
        if (!string.IsNullOrEmpty(result.OldRoomId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(result.OldRoomId));
            await Clients.Group(Group(result.OldRoomId)).SendAsync("UserLeft", new UserLeftDto(result.Session.SessionId, result.Session.DisplayName, result.OldRoomId));
        }

        // 2. Join new room group & notify new room participants
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(result.Session.RoomId));
        var snapshot = Sessions.GetSnapshot(Context.ConnectionId);
        if (snapshot is null) return new ChangeRoomResultDto(false, "Session not found.", null);
        await Clients.GroupExcept(Group(result.Session.RoomId), Context.ConnectionId).SendAsync("UserJoined", new UserJoinedDto(result.Session.ToDto(), result.Session.RoomId));

        return new ChangeRoomResultDto(true, null, snapshot);
    }

    public async Task<MoveResultDto> RequestMove(RequestMoveDto request)
    {
        var session = Sessions.GetPlayer(Context.ConnectionId);
        if (session is null)
        {
            return new MoveResultDto(false, "Session not found. Please join room first.");
        }

        Point2D? start = request.Start is not null ? new Point2D(request.Start.X, request.Start.Y) : null;
        var moveResult = Sessions.RequestMove(Context.ConnectionId, new Point2D(request.Target.X, request.Target.Y), start);
        if (!moveResult.Success || moveResult.Waypoints is null || moveResult.Target is null)
        {
            return new MoveResultDto(false, moveResult.Error ?? "Move request rejected.");
        }

        var message = new PlayerMovedDto(moveResult.SessionId!, moveResult.Target, moveResult.Waypoints, moveResult.RoomId!, moveResult.Revision);
        await Clients.Group(Group(moveResult.RoomId!)).SendAsync("PlayerMoved", message);

        return new MoveResultDto(true, null);
    }

    public async Task<SendChatMessageResultDto> SendChatMessage(SendChatMessageRequestDto request)
    {
        var result = Sessions.SendChatMessage(Context.ConnectionId, request.Text);
        if (!result.Success || result.Message is null)
        {
            return new SendChatMessageResultDto(false, result.Error ?? "Message rejected.", null);
        }

        // Broadcast message to all clients in the same room
        await Clients.Group(Group(result.Message.RoomId)).SendAsync("ChatMessageReceived", result.Message);

        return new SendChatMessageResultDto(true, null, result.Message);
    }

    public Task UpdateProgress(PointDto position, string direction, string state)
    {
        Sessions.UpdatePlayerProgress(Context.ConnectionId, new Point2D(position.X, position.Y), direction, state);
        return Task.CompletedTask;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var left = Sessions.Leave(Context.ConnectionId);
        if (left is not null)
        {
            var userLeft = new UserLeftDto(left.SessionId, left.DisplayName, left.RoomId);
            await Clients.Group(Group(left.RoomId)).SendAsync("UserLeft", userLeft);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(left.RoomId));
        }

        await base.OnDisconnectedAsync(exception);
    }
}
