using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

public partial class RoomSessionManager
{
    private readonly Dictionary<(string Room, string Npc), NpcGuide> _returningGuides = [];

    private sealed class NpcGuide(string home, NpcDefinition npc)
    {
        public string Home { get; } = home;
        public NpcDefinition Npc { get; } = npc;
        public (string Room, string Npc) Key => (Home, Npc.Id);
        public PlayerSession Actor { get; } = new() { RoomId = home, Position = npc.Position };
        public DateTimeOffset RetryAt { get; set; }
        public double FollowDistance => Npc.FollowDistance ?? 42;

        public NpcPresence Presence() => new(Actor.RoomId == Home ? Npc.Id : $"{Home}:{Npc.Id}",
            Npc.Name, Npc.CharacterId, Actor.Position, Actor.Position, new(Actor.Direction, Actor.Waypoints.ToArray()));
    }

    private void SendGuideHome(NpcGuide guide)
    {
        AdvanceMovement(guide.Actor);
        StopTourMovement(guide.Actor);
        _returningGuides.Add(guide.Key, guide);
        _tourNpcRooms.Add(guide.Actor.RoomId);
    }

    private void FinishGuideReturn(NpcGuide guide)
    {
        _returningGuides.Remove(guide.Key);
        _tourNpcRooms.Add(guide.Home);
        _tourNpcRooms.Add(guide.Actor.RoomId);
    }

    private void AdvanceGuideReturn(NpcGuide guide, DateTimeOffset now)
    {
        var actor = guide.Actor;
        AdvanceMovement(actor, now);
        if (actor.State == "walk" || now < guide.RetryAt || _roomService.GetRoom(actor.RoomId) is not { } room) return;

        var route = NpcGuideRouteHome.Find(room, actor.Position, guide.Home, guide.Npc.Position,
            _roomService.GetRoom, _pathfinder, _navigation);
        if (route is null)
        {
            guide.RetryAt = now.AddSeconds(1);
            return;
        }
        if (actor.Position.DistanceTo(route.Point) >= 0.001)
        {
            if (!MovePlayer(actor, room, route.Point, now).Success) guide.RetryAt = now.AddSeconds(1);
        }
        else if (route.ExitId is null)
        {
            FinishGuideReturn(guide);
        }
        else if (TraverseExit(actor, route.ExitId) is null)
        {
            _tourNpcRooms.Add(room.Id);
            _tourNpcRooms.Add(actor.RoomId);
        }
        else guide.RetryAt = now.AddSeconds(1);
    }
}
