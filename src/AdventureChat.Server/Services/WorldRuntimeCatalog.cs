namespace AdventureChat.Server.Services;

// Each world owns its room state, chat history and movement revisions.
public sealed class WorldRuntimeCatalog
{
    public sealed record Runtime(WorldPackage Package, IRoomService Rooms, IRoomSessionManager Sessions);
    private readonly Dictionary<string, Runtime> _worlds = new(StringComparer.OrdinalIgnoreCase);

    public WorldRuntimeCatalog(IEnumerable<WorldPackage> packages, IRoomService firstRooms,
        IRoomSessionManager firstSessions, IPathfindingService pathfinder)
    {
        foreach (var package in packages)
        {
            var rooms = _worlds.Count == 0 ? firstRooms : new RoomService(world: package);
            var sessions = _worlds.Count == 0 ? firstSessions : new RoomSessionManager(rooms, pathfinder);
            _worlds.Add(package.Id, new Runtime(package, rooms, sessions));
        }
    }

    public Runtime? Find(string id) => _worlds.GetValueOrDefault(id);
}
