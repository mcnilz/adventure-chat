namespace AdventureChat.Server.Services;

internal static class DefaultWorld
{
    public static string ResolveReferenceRoot()
    {
        string? bundledRoot = null;
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var root = Path.Combine(directory.FullName, "worlds", "reference");
            if (!File.Exists(Path.Combine(root, "world.yaml"))) continue;
            // Local authoring edits the source world; standalone builds use their bundled copy.
            if (File.Exists(Path.Combine(directory.FullName, "AdventureChat.slnx"))) return root;
            bundledRoot ??= root;
        }
        if (bundledRoot is not null) return bundledRoot;
        throw new InvalidOperationException("Bundled reference world was not found. Supply World:Root to select another world.");
    }
}
