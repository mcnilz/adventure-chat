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
                foreach (var movement in sessions.CollectNpcMovement())
                    await hub.Clients.Group(RoomHub.WorldGroup(world.Package.Id, movement.RoomId))
                        .SendAsync("NpcMovement", movement, stoppingToken);
            }
        }
    }
}
