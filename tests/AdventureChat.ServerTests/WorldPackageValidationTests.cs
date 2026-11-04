using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

[TestFixture]
public class WorldPackageValidationTests
{
    private string _root = null!;

    [Test]
    public void Viewport_LoadsOnlyInNewFormat()
    {
        Replace("rooms/lounge.yaml", "version: 1", "version: 1\nviewport: { width: 320, height: 360, scrollMode: horizontal }");
        Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(_root).LoadRooms());
        Replace("world.yaml", "formatVersion: 1.0.0", "formatVersion: 1.2.0");
        Assert.DoesNotThrow(() => WorldPackage.Load(_root).LoadRooms());
    }

    [TestCase("{ width: 0, height: 360, scrollMode: horizontal }")]
    [TestCase("{ width: 320.5, height: 360, scrollMode: horizontal }")]
    [TestCase("{ width: 320, height: 359, scrollMode: horizontal }")]
    [TestCase("{ width: 320, height: 360, scrollMode: vertical }")]
    public void InvalidViewport_IsRejected(string viewport)
    {
        Replace("world.yaml", "formatVersion: 1.0.0", "formatVersion: 1.2.0");
        Replace("rooms/lounge.yaml", "version: 1", "version: 1\nviewport: " + viewport);
        Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(_root).LoadRooms());
    }

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "adventure-world-" + Guid.NewGuid().ToString("N"));
        var source = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../tests/Fixtures/reference-v1"));
        Copy(source, _root);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_root, true);

    [TestCase("formatVersion: 1.0.0", "formatVersion: 1.99.0", "1.99.0")]
    [TestCase("roomFormat: legacy-1", "roomFormat: 1.0.0", "roomFormat")]
    [TestCase("startSpawn: entry", "startSpawn: absent", "spawn")]
    public void InvalidManifestOrStart_Fails(string oldText, string newText, string diagnostic)
    {
        Replace("world.yaml", oldText, newText);
        var ex = Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(_root).LoadRooms());
        Assert.That(ex!.Message, Does.Contain(diagnostic).IgnoreCase);
    }

    [TestCase("background: assets/images/lounge.png", "background: ../outside.png")]
    [TestCase("version: 1", "version: 2")]
    [TestCase("version: 1", "unknown: true\nversion: 1")]
    public void InvalidRoom_RejectsFileWithDiagnostic(string oldText, string newText)
    {
        Replace("rooms/lounge.yaml", oldText, newText);
        var ex = Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(_root).LoadRooms());
        Assert.That(ex!.Message, Does.Contain("lounge.yaml"));
    }

    [Test]
    public void MissingImage_IsRejected()
    {
        File.Delete(Path.Combine(_root, "assets", "images", "lounge.png"));
        var ex = Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(_root).LoadRooms());
        Assert.That(ex!.Message, Does.Contain("lounge.png"));
    }

    [Test]
    public void ManifestAlias_IsRejected()
    {
        Replace("world.yaml", "name: Referenzwelt", "name: &name Referenzwelt");
        Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(_root));
    }

    [Test]
    public void UnchangedLegacyRooms_LoadWithoutRewriting()
    {
        var file = Path.Combine(_root, "rooms", "lounge.yaml");
        var before = File.ReadAllBytes(file);
        _ = WorldPackage.Load(_root).LoadRooms();
        Assert.That(File.ReadAllBytes(file), Is.EqualTo(before));
    }

    [Test]
    public void Editor_UnsupportedExistingRoom_RefusesToOverwrite()
    {
        var world = WorldPackage.Load(_root);
        var paths = new ContentPathProvider(_root, WorldPackage.FindSchema("room.schema.json"), _root, _root);
        var serializer = new RoomYamlSerializer();
        var validator = new RoomValidator(paths.GetSchemaFilePath());
        var rooms = new RoomService(paths, serializer, validator, world);
        var editor = new EditorService(paths, serializer, validator, rooms, true, world);
        var room = editor.GetRoom("lounge")!;
        Replace("rooms/lounge.yaml", "version: 1", "version: 2");
        var before = File.ReadAllBytes(paths.GetRoomFilePath("lounge"));
        var result = editor.SaveRoom(room);
        Assert.That(result.Success, Is.False);
        Assert.That(File.ReadAllBytes(paths.GetRoomFilePath("lounge")), Is.EqualTo(before));
    }

    [Test]
    public void Editor_SaveThenReload_PreservesWorldAndLegacyVersion()
    {
        var world = WorldPackage.Load(_root);
        var paths = new ContentPathProvider(_root, WorldPackage.FindSchema("room.schema.json"), _root, _root);
        var serializer = new RoomYamlSerializer();
        var validator = new RoomValidator(paths.GetSchemaFilePath());
        var rooms = new RoomService(paths, serializer, validator, world);
        var editor = new EditorService(paths, serializer, validator, rooms, true, world);
        var room = editor.GetRoom("lounge")!;
        room.Notes = "Updated";
        Assert.That(editor.SaveRoom(room).Success, Is.True);
        var reloaded = WorldPackage.Load(_root).LoadRooms().Single(r => r.Id == "lounge");
        Assert.Multiple(() =>
        {
            Assert.That(reloaded.Notes, Is.EqualTo("Updated"));
            Assert.That(reloaded.Version, Is.EqualTo(1));
            Assert.That(WorldPackage.Load(_root).ContentVersion, Is.EqualTo("1.0.0"));
        });
    }

    [Test]
    public void SymlinkedImage_IsRejected()
    {
        var image = Path.Combine(_root, "assets", "images", "lounge.png");
        var outside = Path.Combine(Path.GetTempPath(), "adventure-outside-" + Guid.NewGuid().ToString("N") + ".png");
        File.Move(image, outside);
        try
        {
            try { File.CreateSymbolicLink(image, outside); }
            catch (Exception linkError) when (linkError is UnauthorizedAccessException or IOException) { Assert.Ignore("Creating symbolic links is unavailable in this environment."); }
            var ex = Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(_root).LoadRooms());
            Assert.That(ex!.Message, Does.Contain("Symbolic links"));
        }
        finally { if (File.Exists(image)) File.Delete(image); File.Delete(outside); }
    }

    [TestCase("1.0.0-alpha.1+build.5", true)]
    [TestCase("1.0.0-01", false)]
    [TestCase("1.0", false)]
    public void ContentVersion_UsesSemVer(string version, bool valid)
    {
        Replace("world.yaml", "contentVersion: 1.0.0", "contentVersion: " + version);
        if (valid) Assert.DoesNotThrow(() => WorldPackage.Load(_root));
        else Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(_root));
    }

    [Test]
    public void LinkedImagesDirectory_IsRejected()
    {
        var images = Path.Combine(_root, "assets", "images");
        var relocated = Path.Combine(_root, "relocated-images");
        Directory.Move(images, relocated);
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe", Arguments = $"/c mklink /J \"{images}\" \"{relocated}\"",
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
                });
                process!.WaitForExit();
                if (process.ExitCode != 0) Assert.Ignore("Creating a junction is unavailable in this environment.");
            }
            else Directory.CreateSymbolicLink(images, relocated);
            var ex = Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(_root).LoadRooms());
            Assert.That(ex!.Message, Does.Contain("Symbolic links"));
        }
        finally
        {
            if (Directory.Exists(images)) Directory.Delete(images);
        }
    }

    [Test]
    public void Editor_WhenManifestChanges_RefusesSave()
    {
        var world = WorldPackage.Load(_root);
        var paths = new ContentPathProvider(_root, WorldPackage.FindSchema("room.schema.json"), _root, _root);
        var serializer = new RoomYamlSerializer();
        var validator = new RoomValidator(paths.GetSchemaFilePath());
        var rooms = new RoomService(paths, serializer, validator, world);
        var editor = new EditorService(paths, serializer, validator, rooms, true, world);
        var room = editor.GetRoom("lounge")!;
        Replace("world.yaml", "contentVersion: 1.0.0", "contentVersion: 1.0.1");
        Assert.That(editor.SaveRoom(room).Success, Is.False);
    }

    [Test]
    public void Editor_RejectsRoomThatViolatesPublishedSchema()
    {
        var world = WorldPackage.Load(_root);
        var paths = new ContentPathProvider(_root, WorldPackage.FindSchema("room.schema.json"), _root, _root);
        var serializer = new RoomYamlSerializer();
        var validator = new RoomValidator(paths.GetSchemaFilePath());
        var rooms = new RoomService(paths, serializer, validator, world);
        var editor = new EditorService(paths, serializer, validator, rooms, true, world);
        var room = editor.GetRoom("lounge")!;
        room.Exits![0].Radius = -1;
        Assert.That(editor.SaveRoom(room).Success, Is.False);
        Assert.DoesNotThrow(() => WorldPackage.Load(_root).LoadRooms());
    }

    [Test]
    public void TruncatedPng_IsRejected()
    {
        var image = Path.Combine(_root, "assets", "images", "lounge.png");
        File.WriteAllBytes(image, File.ReadAllBytes(image)[..24]);
        Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(_root).LoadRooms());
    }

    [Test]
    public void ExistingLegacyFiles_AreLoadedByteForByteUnchanged()
    {
        var source = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../content/rooms"));
        foreach (var file in Directory.GetFiles(source, "*.yaml"))
            File.Copy(file, Path.Combine(_root, "rooms", Path.GetFileName(file)), true);
        var lounge = Path.Combine(_root, "rooms", "lounge.yaml");
        var original = File.ReadAllBytes(lounge);
        Assert.That(WorldPackage.Load(_root).LoadRooms().Select(room => room.Id), Does.Contain("lounge"));
        Assert.That(File.ReadAllBytes(lounge), Is.EqualTo(original));
    }

    [Test]
    public void AllRoomSchemas_AreCheckedBeforeImageAndGeometryRules()
    {
        File.Delete(Path.Combine(_root, "assets", "images", "garden.png"));
        Replace("rooms/lounge.yaml", "version: 1", "version: 2");
        var ex = Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(_root).LoadRooms());
        Assert.That(ex!.Message, Does.Contain("lounge.yaml").And.Not.Contain("garden.png"));
    }

    [Test]
    public void ManifestSchema_RejectsInvalidNumericPrerelease()
    {
        var schema = Json.Schema.JsonSchema.FromText(File.ReadAllText(WorldPackage.FindSchema("world.schema.json")));
        var manifest = System.Text.Json.Nodes.JsonNode.Parse("""
            {"formatVersion":"1.0.0","id":"demo","name":"Demo","contentVersion":"1.0.0-01","startRoom":"lounge","startSpawn":"entry","roomFormat":"legacy-1"}
            """);
        Assert.That(schema.Evaluate(manifest).IsValid, Is.False);
    }

    [Test]
    public async Task Server_LoadsLegacyAndCurrentWorldFixturesTogether()
    {
        var solution = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../"));
        var legacyRooms = Path.Combine(solution, "content", "rooms");
        foreach (var file in Directory.GetFiles(legacyRooms, "*.yaml"))
            File.Copy(file, Path.Combine(_root, "rooms", Path.GetFileName(file)), true);
        var second = Path.Combine(_root, "second");
        Copy(Path.Combine(solution, "tests/Fixtures/reference-v1"), second);
        var manifest = Path.Combine(second, "world.yaml");
        File.WriteAllText(manifest, File.ReadAllText(manifest).Replace("id: reference", "id: second"));
        var serverDir = Path.Combine(solution, "src", "AdventureChat.Server");
        var app = AdventureChat.Server.AppFactory.CreateApp([], new Microsoft.AspNetCore.Builder.WebApplicationOptions
        {
            ContentRootPath = serverDir,
            WebRootPath = Path.Combine(serverDir, "wwwroot"),
            Args = ["--urls", "http://127.0.0.1:0", "--World:Roots:0", _root, "--World:Roots:1", second]
        });
        try
        {
            await app.StartAsync();
            var rooms = app.Services.GetService(typeof(IRoomService)) as IRoomService;
            Assert.That(rooms!.GetRoom("lounge"), Is.Not.Null);
        }
        finally { await app.DisposeAsync(); }
    }

    [Test]
    public void DisplayExtension_LoadsOnlyWithNewWorldFormat()
    {
        Replace("rooms/lounge.yaml", "version: 1", "version: 1\nperspective: top-down\nactorHeight: 32\nwalkSpeed: 90");
        Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(_root).LoadRooms());
        Replace("world.yaml", "formatVersion: 1.0.0", "formatVersion: 1.1.0");
        var room = WorldPackage.Load(_root).LoadRooms().Single(r => r.Id == "lounge").ToRoomDefinition();
        Assert.That(System.Text.Json.JsonSerializer.Serialize(room), Does.Contain("top-down").And.Contain("32").And.Contain("90"));
    }

    [TestCase("actorHeight: 15")]
    [TestCase("actorHeight: 129")]
    [TestCase("walkSpeed: 0")]
    [TestCase("perspective: sideways")]
    public void NewDisplayFormat_RejectsInvalidSettings(string setting)
    {
        Replace("world.yaml", "formatVersion: 1.0.0", "formatVersion: 1.1.0");
        Replace("rooms/lounge.yaml", "version: 1", "version: 1\n" + setting);
        Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(_root).LoadRooms());
    }

    [Test]
    public void Editor_NewDisplaySettings_SaveAndReload()
    {
        Replace("world.yaml", "formatVersion: 1.0.0", "formatVersion: 1.1.0");
        var world = WorldPackage.Load(_root);
        var paths = new ContentPathProvider(_root, WorldPackage.FindSchema("room.schema.json", world.FormatVersion), _root, _root);
        var serializer = new RoomYamlSerializer(paths.GetSchemaFilePath(), paths.GetRoomsDirectory());
        var validator = new RoomValidator(paths.GetSchemaFilePath());
        var rooms = new RoomService(paths, serializer, validator, world);
        var editor = new EditorService(paths, serializer, validator, rooms, true, world);
        var room = editor.GetRoom("lounge")!;
        room.Perspective = "top-down";
        room.ActorHeight = 48;
        room.WalkSpeed = 80;
        Assert.That(editor.SaveRoom(room).Success, Is.True);
        var loaded = editor.GetRoom("lounge")!;
        Assert.Multiple(() =>
        {
            Assert.That(loaded.Perspective, Is.EqualTo("top-down"));
            Assert.That(loaded.ActorHeight, Is.EqualTo(48));
            Assert.That(loaded.WalkSpeed, Is.EqualTo(80));
            Assert.That(editor.GetRoomRawYaml("lounge"), Does.Contain("1.1.0/room.schema.json"));
        });
    }

    [Test]
    public void LegacyEditor_RejectsDisplayExtensionWithoutSupportedSchema()
    {
        var validator = new RoomValidator(WorldPackage.FindSchema("room.schema.json"));
        var dto = WorldPackage.Load(_root).LoadRooms().Single(r => r.Id == "lounge");
        dto.Perspective = "top-down";
        Assert.That(validator.Validate(dto).IsValid, Is.False);
    }

    private void Replace(string file, string oldText, string newText)
    {
        var path = Path.Combine(_root, file);
        File.WriteAllText(path, File.ReadAllText(path).Replace(oldText, newText));
    }

    private static void Copy(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        foreach (var dir in Directory.GetDirectories(source)) Copy(dir, Path.Combine(target, Path.GetFileName(dir)));
    }
}
