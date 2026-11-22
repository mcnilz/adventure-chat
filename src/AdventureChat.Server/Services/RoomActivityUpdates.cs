using AdventureChat.Server.Hubs;
using AdventureChat.Server.Models;
using Microsoft.AspNetCore.SignalR;

namespace AdventureChat.Server.Services;

internal sealed class RoomActivityUpdates(WorldRuntimeCatalog worlds, IHubContext<RoomHub> hub) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(40));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var world in worlds.All)
            {
                if (world.Sessions is not RoomSessionManager sessions) continue;
                foreach (var (connectionId, roomId, result) in sessions.CollectActorActivityUpdates())
                {
                    foreach (var change in result.Changes)
                        await hub.Clients.Group(RoomHub.WorldGroup(world.Package.Id, change.RoomId))
                            .SendAsync("ObjectStatesChanged", change, stoppingToken);
                    if (result.Error is { } error)
                        await hub.Clients.Client(connectionId).SendAsync("ObjectActionFeedback",
                            new ObjectActionFeedbackDto(roomId, error), stoppingToken);
                }
                foreach (var tour in sessions.CollectNpcTourUpdates())
                    await PublishTourUpdate(world, sessions, tour, stoppingToken);
                foreach (var movement in sessions.CollectNpcMovement())
                    await hub.Clients.Group(RoomHub.WorldGroup(world.Package.Id, movement.RoomId))
                        .SendAsync("NpcMovement", movement, stoppingToken);
            }
        }
    }

    private async Task PublishTourUpdate(WorldRuntimeCatalog.Runtime world, RoomSessionManager sessions,
        NpcTourUpdate update, CancellationToken token)
    {
        string Group(string roomId) => RoomHub.WorldGroup(world.Package.Id, roomId);
        var guest = update.Guest;
        var connectionId = guest.ConnectionId;
        if (update.LeftRoom is { } left)
        {
            await guest.RoomMembership.WaitAsync(token);
            try
            {
                if (sessions.GetPlayer(connectionId) == guest)
                    await hub.Clients.Client(connectionId).SendAsync("NpcTourRoomChanging", token);
                var snapshot = await RoomTransfer.Publish(sessions, hub.Groups, hub.Clients, Group, guest, left, token);
                if (snapshot is not null)
                    await hub.Clients.Client(connectionId).SendAsync("NpcTourRoomChanged", snapshot, token);
            }
            finally
            {
                sessions.CompleteTourTransfer(guest);
                guest.RoomMembership.Release();
            }
        }
        if (update.Move is { } move)
            await hub.Clients.Group(Group(move.RoomId!)).SendAsync("PlayerMoved", move.ToMovementDto(), token);
        if (update.Status is { } status && sessions.GetPlayer(connectionId) == guest)
            await hub.Clients.Client(connectionId).SendAsync("NpcTourStatus", status, token);
    }
}
