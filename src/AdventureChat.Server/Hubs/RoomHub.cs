using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace AdventureChat.Server.Hubs;

public class RoomHub : Hub
{
    private readonly IRoomSessionManager _sessionManager;

    public RoomHub(IRoomSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public async Task<JoinRoomResultDto> JoinRoom(JoinRoomRequestDto request)
    {
        var result = _sessionManager.JoinRoom(Context.ConnectionId, request.RoomId, request.DisplayName);
        if (!result.Success || result.Session is null || result.Snapshot is null)
        {
            return new JoinRoomResultDto(false, result.Error ?? "Could not join room.", null);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, request.RoomId);

        // Notify other players in the room about the new participant
        await Clients.OthersInGroup(request.RoomId).SendAsync("UserJoined", new UserJoinedDto(result.Session.ToDto(), request.RoomId));

        return new JoinRoomResultDto(true, null, result.Snapshot);
    }

    public async Task<ChangeRoomResultDto> ChangeRoom(ChangeRoomRequestDto request)
    {
        var result = _sessionManager.ChangeRoom(Context.ConnectionId, request.ExitId);
        if (!result.Success || result.Session is null || result.Snapshot is null)
        {
            return new ChangeRoomResultDto(false, result.Error ?? "Could not change room.", null);
        }

        // 1. Leave old room group & notify old room participants
        if (!string.IsNullOrEmpty(result.OldRoomId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, result.OldRoomId);
            await Clients.Group(result.OldRoomId).SendAsync("UserLeft", new UserLeftDto(result.Session.SessionId, result.Session.DisplayName, result.OldRoomId));
        }

        // 2. Join new room group & notify new room participants
        await Groups.AddToGroupAsync(Context.ConnectionId, result.Session.RoomId);
        await Clients.GroupExcept(result.Session.RoomId, Context.ConnectionId).SendAsync("UserJoined", new UserJoinedDto(result.Session.ToDto(), result.Session.RoomId));

        return new ChangeRoomResultDto(true, null, result.Snapshot);
    }

    public async Task<MoveResultDto> RequestMove(RequestMoveDto request)
    {
        var session = _sessionManager.GetPlayer(Context.ConnectionId);
        if (session is null)
        {
            return new MoveResultDto(false, "Session not found. Please join room first.");
        }

        Point2D? start = request.Start is not null ? new Point2D(request.Start.X, request.Start.Y) : null;
        var moveResult = _sessionManager.RequestMove(Context.ConnectionId, new Point2D(request.Target.X, request.Target.Y), start);
        if (!moveResult.Success || moveResult.Waypoints is null || moveResult.Target is null)
        {
            return new MoveResultDto(false, moveResult.Error ?? "Move request rejected.");
        }

        var message = new PlayerMovedDto(moveResult.SessionId!, moveResult.Target, moveResult.Waypoints, moveResult.RoomId!, moveResult.Revision);
        await Clients.Group(moveResult.RoomId!).SendAsync("PlayerMoved", message);

        return new MoveResultDto(true, null);
    }

    public async Task<SendChatMessageResultDto> SendChatMessage(SendChatMessageRequestDto request)
    {
        var result = _sessionManager.SendChatMessage(Context.ConnectionId, request.Text);
        if (!result.Success || result.Message is null)
        {
            return new SendChatMessageResultDto(false, result.Error ?? "Message rejected.", null);
        }

        // Broadcast message to all clients in the same room
        await Clients.Group(result.Message.RoomId).SendAsync("ChatMessageReceived", result.Message);

        return new SendChatMessageResultDto(true, null, result.Message);
    }

    public Task UpdateProgress(PointDto position, string direction, string state)
    {
        _sessionManager.UpdatePlayerProgress(Context.ConnectionId, new Point2D(position.X, position.Y), direction, state);
        return Task.CompletedTask;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var left = _sessionManager.Leave(Context.ConnectionId);
        if (left is not null)
        {
            var userLeft = new UserLeftDto(left.SessionId, left.DisplayName, left.RoomId);
            await Clients.Group(left.RoomId).SendAsync("UserLeft", userLeft);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, left.RoomId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
