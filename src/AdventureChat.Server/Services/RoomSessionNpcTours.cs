using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

internal sealed record NpcTourStatus(string SessionId, bool Active, string Message, NpcTourSpeech? Speech = null);
internal sealed record NpcTourSpeech(string NpcId, int DurationMs);
internal sealed record NpcTourUpdate(PlayerSession Guest, MoveResult? Move = null, NpcTourStatus? Status = null,
    string? LeftRoom = null);

public partial class RoomSessionManager
{
    private static readonly AvailableDialogueOption TourOffer = new(":tour", "Führung starten", "");
    private static readonly TimeSpan TourLead = TimeSpan.FromMilliseconds(300);
    private readonly Dictionary<string, NpcTour> _npcTours = [];
    private readonly HashSet<string> _tourTransfers = [];
    private readonly HashSet<string> _tourNpcRooms = [];
    private readonly List<NpcTourUpdate> _tourUpdates = [];

    private enum TourPhase { StartStep, Follow, Arrive, Explain }

    private sealed class NpcTour(PlayerSession guest, RoomDefinition home, NpcDefinition npc)
    {
        public PlayerSession Guest { get; } = guest;
        public NpcGuide Guide { get; } = new(home.Id, npc);
        public NpcTourStep Step => Guide.Npc.Tour![StepIndex];
        public int StepIndex { get; set; }
        public TourPhase Phase { get; set; }
        public DateTimeOffset DueAt { get; set; }
        public NpcTourTrail? Trail { get; set; }
    }

    private bool TourControlsPlayer(string connectionId) =>
        _npcTours.ContainsKey(connectionId) || _tourTransfers.Contains(connectionId);

    private bool GuideIsBusy(string roomId, string npcId) =>
        _returningGuides.ContainsKey((roomId, npcId)) || _npcTours.Values.Any(tour => tour.Guide.Key == (roomId, npcId));

    private IEnumerable<NpcGuide> TourGuides() =>
        _npcTours.Values.Select(tour => tour.Guide).Concat(_returningGuides.Values);

    private NpcDialogueResult StartTour(PlayerSession guest, RoomDefinition room, NpcDefinition npc)
    {
        if (guest.ObjectAnimation is not null)
            return new("Beende zuerst die aktuelle Benutzung.");
        guest.AnimationApproach = null;
        var tour = new NpcTour(guest, room, npc);
        var patrol = UpdateNpcPatrols(room, NpcNow).FirstOrDefault(candidate => candidate.Id == npc.Id);
        if (patrol is not null)
        {
            tour.Guide.Actor.Position = patrol.Position;
            patrol.HandOffToGuide();
            foreach (var approach in _npcApproaches.Where(pair => pair.Value.RoomId == room.Id &&
                pair.Value.NpcId == npc.Id).Select(pair => pair.Key).ToArray())
                _npcApproaches.Remove(approach);
        }
        _npcTours.Add(guest.ConnectionId, tour);
        _tourNpcRooms.Add(room.Id);
        _tourUpdates.Add(new(guest, Status: new(guest.SessionId, true, $"Folge {npc.Name}.")));
        return new(null, TourStarted: true);
    }

    public void CancelNpcTour(string connectionId)
    {
        lock (_lock)
        {
            if (_npcTours.TryGetValue(connectionId, out var tour))
                StopTour(tour, "Führung beendet.");
        }
    }

    private void StopTour(NpcTour tour, string message)
    {
        var guest = tour.Guest;
        AdvanceMovement(guest);
        _tourUpdates.Add(new(guest, Move: StopTourMovement(guest), Status: new(guest.SessionId, false, message)));
        ReleaseTour(guest.ConnectionId);
    }

    private static MoveResult StopTourMovement(PlayerSession player)
    {
        player.State = "idle";
        player.Target = null;
        player.Waypoints.Clear();
        player.NavigationStopped = false;
        var point = new PointDto(player.Position.X, player.Position.Y);
        return new(true, null, player.SessionId, point, [point], player.RoomId, ++player.MovementRevision);
    }

    private void ReleaseTour(string connectionId)
    {
        if (!_npcTours.Remove(connectionId, out var tour)) return;
        SendGuideHome(tour.Guide);
    }

    internal IReadOnlyList<NpcTourUpdate> CollectNpcTourUpdates()
    {
        lock (_lock)
        {
            var now = _clock.GetUtcNow();
            foreach (var guide in _returningGuides.Values.ToArray())
                AdvanceGuideReturn(guide, now);
            foreach (var tour in _npcTours.Values.ToArray())
                AdvanceTour(tour, now);
            var updates = _tourUpdates.ToArray();
            _tourUpdates.Clear();
            return updates;
        }
    }

    internal void CompleteTourTransfer(PlayerSession guest)
    {
        lock (_lock)
        {
            if (_sessionsByConnection.GetValueOrDefault(guest.ConnectionId) == guest)
                _tourTransfers.Remove(guest.ConnectionId);
        }
    }

    private void AdvanceTour(NpcTour tour, DateTimeOffset now)
    {
        var guest = tour.Guest;
        if (_tourTransfers.Contains(guest.ConnectionId)) return;
        var guide = tour.Guide.Actor;
        var room = _roomService.GetRoom(guide.RoomId);
        if (room is null)
        {
            StopTour(tour, "Die Führung wurde unterbrochen.");
            return;
        }
        var exit = tour.Step.Exit is { } id
            ? room.Exits.FirstOrDefault(exit => string.Equals(exit.Id, id, StringComparison.OrdinalIgnoreCase))
            : null;
        if (tour.Step.Exit is not null && (exit is null || !_navigation.ExitUsable(room, exit)))
        {
            StopTour(tour, "Die Führung endet hier. Der Durchgang ist versperrt.");
            return;
        }
        AdvanceMovement(guide, now);
        AdvanceMovement(guest, now);

        var destination = exit?.Position ?? tour.Step.Position!.Value;
        switch (tour.Phase)
        {
            case TourPhase.StartStep:
                StartGuideStep(tour, room, destination, exit, now);
                break;
            case TourPhase.Follow when now >= tour.DueAt:
                var route = tour.Trail!.Route;
                _tourUpdates.Add(new(guest, Move: BeginMovement(guest, route[^1], route, now)));
                tour.Phase = TourPhase.Arrive;
                tour.Trail = null;
                break;
            case TourPhase.Arrive when guide.State == "idle" && guest.State == "idle":
                if (exit is not null) TransferTour(tour, exit);
                else ExplainStation(tour, now);
                break;
            case TourPhase.Explain when now >= tour.DueAt:
                if (tour.StepIndex + 1 == tour.Guide.Npc.Tour!.Count) StopTour(tour, "Die Führung ist abgeschlossen.");
                else NextStep(tour);
                break;
        }
    }

    private void StartGuideStep(NpcTour tour, RoomDefinition room, Point2D destination, RoomExit? exit, DateTimeOffset now)
    {
        var guide = tour.Guide.Actor;
        var distance = tour.Guide.FollowDistance;
        var stopDistance = Math.Min(exit?.Radius ?? distance, distance);
        var geometry = _navigation.Geometry(room);
        var guideRoute = _pathfinder.CalculatePath(geometry, guide.Position, destination);
        if (guideRoute.Waypoints is not { } path ||
            NpcTourTrail.Plan(_pathfinder, geometry, path, tour.Guest.Position, stopDistance) is not { } trail)
        {
            StopTour(tour, "Die Führung endet hier. Der Weg ist versperrt.");
            return;
        }
        BeginMovement(guide, destination, path, now);
        tour.Trail = trail;
        tour.Phase = TourPhase.Follow;
        var lead = Math.Min(path.Zip(path.Skip(1)).Sum(leg => leg.First.DistanceTo(leg.Second)),
            trail.JoinDistance + distance) / room.WalkSpeed;
        tour.DueAt = now.AddSeconds(Math.Max(TourLead.TotalSeconds, lead));
    }

    private void RepairTourOnNavigationChange(NpcTour tour, RoomGeometry geometry, DateTimeOffset now)
    {
        var guide = tour.Guide.Actor;
        var guest = tour.Guest;
        AdvanceMovement(guide, now);
        if (RouteIsUsable(guide.Waypoints, geometry) && RouteIsUsable(guest.Waypoints, geometry) &&
            (tour.Trail is null || RouteIsUsable(tour.Trail.Route, geometry))) return;
        StopTourMovement(guide);
        _tourUpdates.Add(new(guest, Move: StopTourMovement(guest)));
        tour.Phase = TourPhase.StartStep;
        tour.Trail = null;
    }

    private void ExplainStation(NpcTour tour, DateTimeOffset now)
    {
        tour.Phase = TourPhase.Explain;
        var text = tour.Step.Text;
        var duration = text is null ? 700 : Math.Clamp(1000 + text.Length * 45, 2500, 12000);
        tour.DueAt = now.AddMilliseconds(duration);
        if (text is not null)
            _tourUpdates.Add(new(tour.Guest, Status: new(tour.Guest.SessionId, true, text,
                new(tour.Guide.Npc.Id, duration))));
    }

    private void TransferTour(NpcTour tour, RoomExit exit)
    {
        var guest = tour.Guest;
        var left = guest.RoomId;
        if (TraverseExit(guest, exit.Id) is { } error)
        {
            StopTour(tour, error);
            return;
        }
        PlaceInRoom(tour.Guide.Actor, guest.RoomId, guest.Position);
        _tourNpcRooms.Add(left);
        _tourNpcRooms.Add(guest.RoomId);
        _tourTransfers.Add(guest.ConnectionId);
        _tourUpdates.Add(new(guest, LeftRoom: left));
        NextStep(tour);
    }

    private static void NextStep(NpcTour tour)
    {
        tour.StepIndex++;
        tour.Phase = TourPhase.StartStep;
    }
}
