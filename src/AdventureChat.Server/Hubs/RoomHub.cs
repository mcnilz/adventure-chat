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
    private string Group(string roomId) => CurrentWorld is { } world ? WorldGroup(world.Package.Id, roomId) : roomId;
    internal static string WorldGroup(string worldId, string roomId) => $"world:{worldId}:room:{roomId.ToLowerInvariant()}";
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
        var characterId = request.CharacterId ?? selected?.Package.DefaultCharacterId;
        if (characterId is not null && (selected is null || !selected.Package.Characters.Any(c => c.Id == characterId && !c.NpcOnly)))
            return new JoinRoomResultDto(false, "Figur nicht verfügbar. Bitte erneut auswählen.", null);
        if (selected is not null) Context.Items["world"] = selected;
        var result = Sessions.JoinRoom(Context.ConnectionId, request.RoomId, request.DisplayName, characterId: characterId);
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

        var snapshot = await RoomTransfer.Publish(Sessions, Groups, Clients, Group, result.Session, result.OldRoomId!);
        return snapshot is null ? new(false, "Session not found.", null) : new(true, null, snapshot);
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
        if (moveResult.AnimationChange is { } change)
            await Clients.Group(Group(change.RoomId)).SendAsync("ObjectStatesChanged", change);
        if (!moveResult.Success || moveResult.Waypoints is null || moveResult.Target is null)
        {
            return new MoveResultDto(false, moveResult.Error ?? "Move request rejected.");
        }

        var message = moveResult.ToMovementDto();
        await Clients.Group(Group(moveResult.RoomId!)).SendAsync("PlayerMoved", message);

        return new MoveResultDto(true, null);
    }

    public async Task<SendChatMessageResultDto> SendChatMessage(SendChatMessageRequestDto request)
    {
        var sessions = Sessions;
        if (sessions.GetPlayer(Context.ConnectionId) is not { } player)
            return new(false, "Player session not found. Please join room first.", null);
        await player.RoomMembership.WaitAsync();
        try
        {
            var result = sessions.SendChatMessage(Context.ConnectionId, request.Text, request.RoomId);
            if (!result.Success || result.Message is null)
                return new(false, result.Error ?? "Message rejected.", null);
            await Clients.Group(Group(result.Message.RoomId)).SendAsync("ChatMessageReceived", result.Message);
            return new(true, null, result.Message);
        }
        finally
        {
            player.RoomMembership.Release();
        }
    }

    public async Task<ObjectActionResultDto> RequestObjectAction(ObjectActionRequestDto request)
    {
        var result = Sessions.RequestObjectAction(Context.ConnectionId, request);
        if (result.Move is { } move)
            await Clients.Group(Group(move.RoomId!)).SendAsync("PlayerMoved", move.ToMovementDto());
        foreach (var change in result.Changes)
            await Clients.Group(Group(change.RoomId)).SendAsync("ObjectStatesChanged", change);
        return new ObjectActionResultDto(result.Success, result.Error, result.ErrorCode, result.Pending);
    }

    public void CancelObjectApproach() => Sessions.CancelObjectApproach(Context.ConnectionId);

    public NpcDialogueResult RequestNpcDialogue(NpcDialogueRequest request) =>
        Sessions.RequestNpcDialogue(Context.ConnectionId, request, allowTours: CurrentWorld is not null);

    public void CancelNpcTour() => Sessions.CancelNpcTour(Context.ConnectionId);

    public void EndNpcDialogue(string approachId) => Sessions.EndNpcDialogue(Context.ConnectionId, approachId);

    public Task UpdateProgress(PointDto position, string direction, string state)
    {
        Sessions.UpdatePlayerProgress(Context.ConnectionId, new Point2D(position.X, position.Y), direction, state);
        return Task.CompletedTask;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveWorld();
        await base.OnDisconnectedAsync(exception);
    }

    public async Task LeaveWorld()
    {
        var sessions = Sessions;
        if (sessions.GetPlayer(Context.ConnectionId) is { } player)
        {
            await player.RoomMembership.WaitAsync();
            try
            {
                if (sessions.GetPlayer(Context.ConnectionId) == player && sessions.Leave(Context.ConnectionId) is { } left)
                {
                    var userLeft = new UserLeftDto(left.SessionId, left.DisplayName, left.RoomId);
                    await Clients.Group(Group(left.RoomId)).SendAsync("UserLeft", userLeft);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(left.RoomId));
                }
            }
            finally
            {
                player.RoomMembership.Release();
            }
        }

        Context.Items.Remove("world");
    }
}
