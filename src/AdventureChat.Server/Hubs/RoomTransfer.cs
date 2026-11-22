using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace AdventureChat.Server.Hubs;

internal static class RoomTransfer
{
    internal static async Task<RoomSnapshotDto?> Publish(IRoomSessionManager sessions, IGroupManager groups,
        IHubClients<IClientProxy> clients, Func<string, string> group, PlayerSession traveller, string oldRoomId,
        CancellationToken cancellationToken = default)
    {
        var connectionId = traveller.ConnectionId;
        await groups.RemoveFromGroupAsync(connectionId, group(oldRoomId), cancellationToken);
        await clients.Group(group(oldRoomId)).SendAsync("UserLeft", new UserLeftDto(traveller.SessionId, traveller.DisplayName, oldRoomId),
            cancellationToken);
        if (sessions.GetPlayer(connectionId) != traveller) return null;
        await groups.AddToGroupAsync(connectionId, group(traveller.RoomId), cancellationToken);
        var snapshot = sessions.GetSnapshot(connectionId);
        if (snapshot is null) return null;
        var player = snapshot.Players.Single(player => player.SessionId == snapshot.YourSessionId);
        await clients.GroupExcept(group(snapshot.RoomId), [connectionId]).SendAsync("UserJoined",
            new UserJoinedDto(player, snapshot.RoomId), cancellationToken);
        return snapshot;
    }
}
