using AdventureChat.Server.Services;
using AdventureChat.Server.Models;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

public class InteractiveObjectTests
{
    [Test]
    public void MultipleUsePoints_ValidateBoundsAndSurviveRoundtrip()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../tests/Fixtures/interactions"));
        var room = WorldPackage.Load(root).LoadRooms().Single(r => r.Id == "hall");
        var obj = room.Objects![0];
        obj.UsePoints = [new() { X = 100, Y = 300 }, new() { X = 350, Y = 300 }];
        var validator = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.7.0"));
        Assert.That(validator.Validate(room).IsValid, Is.True);
        var copy = RoomYamlDto.FromRoomDefinition(room.ToRoomDefinition());
        Assert.That(copy.Objects![0].UsePoints!.Select(p => p.X), Is.EqualTo(new[] { 100d, 350d }));
        obj.UsePoints = [new() { X = 100, Y = 300 }, new() { X = 100, Y = 300 }];
        Assert.That(validator.Validate(room).IsValid, Is.False);
        obj.UsePoints = Enumerable.Range(0, 9).Select(i => new RoomPointYamlDto { X = 100 + i, Y = 300 }).ToList();
        Assert.That(validator.Validate(room).IsValid, Is.False);
        obj.UsePoints = [new() { X = -1, Y = 300 }];
        Assert.That(validator.Validate(room).IsValid, Is.False);
        obj.UsePoints = [new() { X = 100, Y = 300 }];
        Assert.That(new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.6.0")).Validate(room).IsValid, Is.False);
    }

    [Test]
    public void MultipleUsePoints_LoadWithoutLegacyUsePoint()
    {
        var source = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../tests/Fixtures/interactions"));
        var root = Path.Combine(Path.GetTempPath(), "multi-use-world-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var destination = Path.Combine(root, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(file, destination);
            }
            var manifest = Path.Combine(root, "world.yaml");
            File.WriteAllText(manifest, File.ReadAllText(manifest).Replace("1.6.0", "1.7.0"));
            var hall = Path.Combine(root, "rooms", "hall.yaml");
            var yaml = File.ReadAllText(hall).Replace("    depthY: 280\n    interaction:", "    depthY: 280\n    usePoints: [{x: 150, y: 300}, {x: 300, y: 300}]\n    interaction:");
            var point = "      usePoint: { x: 100, y: 300 }\n";
            var index = yaml.IndexOf(point, StringComparison.Ordinal);
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            File.WriteAllText(hall, yaml.Remove(index, point.Length));
            var loaded = WorldPackage.Load(root).LoadRooms();
            Assert.That(loaded, Has.Count.EqualTo(2));
            var serializer = new RoomYamlSerializer();
            var saved = serializer.Serialize(RoomYamlDto.FromRoomDefinition(loaded.Single(r => r.Id == "hall").ToRoomDefinition()));
            var reopened = serializer.Deserialize(saved).Objects!.Single(o => o.Id == "chair");
            Assert.That(reopened.UsePoints, Has.Count.EqualTo(2));
            Assert.That(reopened.Interaction!.UsePoint, Is.Null);
            File.WriteAllText(hall, saved);
            Assert.That(WorldPackage.Load(root).LoadRooms(), Has.Count.EqualTo(2));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void MultipleObjectUsePoints_AreAcceptedOnlyByTheirNewFormat()
    {
        const string json = """
            {"version":1,"id":"hall","name":"Hall","background":"assets/images/bg.png",
             "size":{"width":640,"height":360},"spawns":[{"id":"entry","position":{"x":100,"y":300}}],
             "walkable":[{"id":"floor","points":[{"x":0,"y":0},{"x":640,"y":0},{"x":640,"y":360},{"x":0,"y":360}]}],
             "objects":[{"id":"table","image":"assets/images/table.png","position":{"x":200,"y":200},"size":{"width":100,"height":100},"depthY":280,
               "usePoints":[{"x":180,"y":300},{"x":320,"y":300}]}]}
            """;
        var current = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.7.0"));
        var older = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.6.0"));
        Assert.That(current.ValidateJson(json).IsValid, Is.True);
        Assert.That(older.ValidateJson(json).IsValid, Is.False);
    }

    [Test]
    public void DemonstrationWorld_LoadsBothRoomsAndLinkedActions()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../tests/Fixtures/interactions"));
        var rooms = WorldPackage.Load(root).LoadRooms();
        Assert.That(rooms, Has.Count.EqualTo(2));
        Assert.That(rooms.Single(r => r.Id == "hall").Objects!.Single(o => o.Id == "switch").Interaction!.Actions[0].Effects![0].Object, Is.EqualTo("gate"));
    }

    [Test]
    public void Authoring_RejectsContradictoryAssignmentsAndPreservesValidRoundtrip()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../tests/Fixtures/interactions"));
        var rooms = WorldPackage.Load(root).LoadRooms();
        var hall = rooms.Single(r => r.Id == "hall");
        var serializer = new RoomYamlSerializer();
        var roundtrip = serializer.Deserialize(serializer.Serialize(RoomYamlDto.FromRoomDefinition(hall.ToRoomDefinition())));
        Assert.That(roundtrip.Objects!.Single(o => o.Id == "switch").Interaction!.Actions[0].Effects![0].State, Is.EqualTo("open"));
        var effects = hall.Objects!.Single(o => o.Id == "switch").Interaction!.Actions[0].Effects!;
        effects.Add(new ObjectEffect { Room = "next", Object = "gate", State = "closed" });
        var validator = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.6.0"));
        Assert.That(validator.ValidateWorld(rooms).IsValid, Is.False);
    }

    [Test]
    public void Authoring_RejectsMissingStateImageAndUnknownLinkedObject()
    {
        var fixture = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../tests/Fixtures/foreground"));
        var world = WorldPackage.Load(fixture);
        var rooms = world.LoadRooms();
        var room = rooms.Single(r => r.Id == "hall");
        room.Objects![0].Interaction = new ObjectInteraction
        {
            Name = "Table", InitialState = "closed", UsePoint = new() { X = 100, Y = 300 }, UseRadius = 40,
            States = [new() { Id = "closed", Name = "Closed", Image = "assets/images/missing.png", Position = new() { X = 200, Y = 200 }, Size = new() { Width = 120, Height = 120 }, DepthY = 280 }],
            Actions = [new() { Id = "open", Name = "Open", From = "closed", To = "closed", Effects = [new() { Room = "next", Object = "missing", State = "open" }] }]
        };
        var validator = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.6.0"));
        Assert.That(validator.ValidateWorld(rooms).IsValid, Is.False);
        Assert.Throws<InvalidOperationException>(() => world.ValidateBackground(room));
    }

    [Test]
    public void Authoring_RejectsAnUnknownInitialState()
    {
        var fixture = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../tests/Fixtures/foreground"));
        var room = WorldPackage.Load(fixture).LoadRooms().Single(r => r.Id == "hall");
        room.Objects![0].Interaction = new ObjectInteraction
        {
            Name = "Table", InitialState = "missing", UsePoint = new() { X = 100, Y = 300 }, UseRadius = 40,
            States = [new() { Id = "closed", Name = "Closed", Image = "assets/images/table.png", Position = new() { X = 200, Y = 200 }, Size = new() { Width = 120, Height = 120 }, DepthY = 280 }],
            Actions = [new() { Id = "open", Name = "Open", From = "closed", To = "closed" }]
        };
        var validator = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.6.0"));
        Assert.That(validator.Validate(room).IsValid, Is.False);
    }

    [Test]
    public void InteractionFields_AreAcceptedOnlyInNewSchema()
    {
        const string json = """
            {"version":1,"id":"hall","name":"Hall","background":"assets/images/bg.png",
             "size":{"width":640,"height":360},"spawns":[{"id":"entry","position":{"x":100,"y":300}}],
             "walkable":[{"id":"floor","points":[{"x":0,"y":0},{"x":640,"y":0},{"x":640,"y":360},{"x":0,"y":360}]}],
             "objects":[{"id":"door","image":"assets/images/door.png","position":{"x":200,"y":200},"size":{"width":100,"height":100},"depthY":280,
               "interaction":{"name":"Door","initialState":"closed","usePoint":{"x":180,"y":300},"useRadius":40,
                 "states":[{"id":"closed","name":"Closed","image":"assets/images/door.png","position":{"x":200,"y":200},"size":{"width":100,"height":100},"depthY":280}],
                 "actions":[{"id":"open","name":"Open","from":"closed","to":"closed"}]}}]}
            """;
        var current = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.6.0"));
        var older = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.5.0"));
        Assert.That(current.ValidateJson(json).IsValid, Is.True);
        Assert.That(older.ValidateJson(json).IsValid, Is.False);
    }

    [Test]
    public void Format160_IsExplicitlySupported()
    {
        var source = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../tests/Fixtures/foreground"));
        var root = Path.Combine(Path.GetTempPath(), "interactive-world-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var destination = Path.Combine(root, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(file, destination);
            }
            var manifest = Path.Combine(root, "world.yaml");
            File.WriteAllText(manifest, File.ReadAllText(manifest).Replace("1.4.0", "1.6.0"));

            Assert.That(WorldPackage.Load(root).LoadRooms(), Is.Not.Empty);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
