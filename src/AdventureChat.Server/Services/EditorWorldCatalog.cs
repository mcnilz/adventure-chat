namespace AdventureChat.Server.Services;

// Authoring services share the same room services used by game sessions.
public sealed class EditorWorldCatalog
{
    private readonly Dictionary<string, IEditorService> _editors = new(StringComparer.OrdinalIgnoreCase);
    private readonly IEditorService _defaultEditor;

    public EditorWorldCatalog(IEnumerable<WorldPackage> worlds, WorldRuntimeCatalog runtimes,
        IEditorService defaultEditor, bool authoring)
    {
        _defaultEditor = defaultEditor;
        foreach (var world in worlds)
        {
            var paths = new ContentPathProvider(world.Root, WorldPackage.FindSchema("room.schema.json"), world.Root, world.Root);
            _editors.Add(world.Id, new EditorService(paths,
                new RoomYamlSerializer(WorldPackage.FindSchema("room.schema.json"), paths.GetRoomsDirectory()),
                new RoomValidator(paths.GetSchemaFilePath()), runtimes.Find(world.Id)!.Rooms, authoring, world));
        }
    }

    public IEditorService? Find(string? id) => string.IsNullOrEmpty(id) ? _defaultEditor : _editors.GetValueOrDefault(id);
}
