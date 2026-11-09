using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

public class DepthScaleTests
{
    [TestCase("map-v1", "1.1.0")]
    [TestCase("scroll-v1", "1.2.0")]
    public void FrozenWorlds_KeepEarlierFormatAndFixedSizes(string fixture, string version)
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../tests/Fixtures", fixture));
        var world = WorldPackage.Load(root);
        Assert.That(world.FormatVersion, Is.EqualTo(version));
        foreach (var room in world.LoadRooms())
        {
            Assert.That(room.DepthScale, Is.Null);
            Assert.That(room.ToRoomDefinition().ToDto().DepthScale, Is.Null);
            Assert.That(room.ToRoomDefinition().ActorHeight, Is.EqualTo(room.ActorHeight ?? 64));
        }
    }

    [Test]
    public void Catalog_AcceptsOptionalTopDownSheetOnlyInNewFormat()
    {
        var source = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../tests/Fixtures/characters-a"));
        var root = Path.Combine(Path.GetTempPath(), "depth-catalog-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(root, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(file, target);
            }
            var manifest = Path.Combine(root, "world.yaml");
            var json = System.Text.Json.Nodes.JsonNode.Parse(new YamlDotNet.Serialization.SerializerBuilder().JsonCompatible().Build().Serialize(new YamlDotNet.Serialization.DeserializerBuilder().WithAttemptingUnquotedStringTypeDeserialization().Build().Deserialize<object>(File.ReadAllText(manifest))))!;
            json["formatVersion"] = "1.5.0";
            json["characters"]![0]!["topDownImage"] = json["characters"]![0]!["image"]!.GetValue<string>();
            File.WriteAllText(manifest, json.ToJsonString());
            Assert.DoesNotThrow(() => WorldPackage.Load(root));
            json["characters"]![0]!["topDownImage"] = "assets/images/missing.png";
            File.WriteAllText(manifest, json.ToJsonString());
            Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(root));
            json["formatVersion"] = "1.4.0";
            json["characters"]![0]!["topDownImage"] = json["characters"]![0]!["image"]!.GetValue<string>();
            File.WriteAllText(manifest, json.ToJsonString());
            Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(root));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [TestCase("reference")]
    [TestCase("map")]
    [TestCase("scroll")]
    public void ShippedWorld_ProvidesReachableScrollDepthAndCombinedExamples(string id)
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../worlds", id));
        var rooms = WorldPackage.Load(root).LoadRooms().Select(r => r.ToRoomDefinition()).ToDictionary(r => r.Id);
        var map = rooms["tour-map"];
        Assert.That(map.Perspective, Is.EqualTo("top-down"));
        Assert.That(map.DepthScale, Is.Null);
        Assert.That(map.Viewport!.Width, Is.LessThan(map.Width));
        Assert.That(rooms["tour-depth"].Viewport, Is.Null);
        foreach (var key in new[] { "tour-depth", "tour-combined" })
        {
            var room = rooms[key];
            Assert.That(room.DepthScale, Is.Not.Null);
            var pathfinder = new PathfindingService();
            Assert.That(pathfinder.CalculatePath(room.Geometry, room.SpawnPoint, new(320, 140)).Success, Is.True, key);
            Assert.That(pathfinder.CalculatePath(room.Geometry, new(320, 140), new(room.Width - 100, 325)).Success, Is.True, key);
        }
        Assert.That(rooms["tour-combined"].Viewport!.Width, Is.LessThan(rooms["tour-combined"].Width));
        var reachable = new HashSet<string>();
        void Visit(string room) { if (reachable.Add(room)) foreach (var exit in rooms[room].Exits) Visit(exit.TargetRoomId); }
        Visit(WorldPackage.Load(root).StartRoom);
        Assert.That(reachable, Does.Contain("tour-map").And.Contain("tour-depth").And.Contain("tour-combined"));
    }

    [TestCase(120, 320, 0.5, 1, true)]
    [TestCase(0, 360, 0.25, 2, true)]
    [TestCase(120, 120, 0.5, 1, false)]
    [TestCase(320, 120, 0.5, 1, false)]
    [TestCase(-1, 320, 0.5, 1, false)]
    [TestCase(120, 361, 0.5, 1, false)]
    [TestCase(120, 320, 0, 1, false)]
    [TestCase(120, 320, 0.5, 2.01, false)]
    [TestCase(120, 320, double.NaN, 1, false)]
    [TestCase(double.PositiveInfinity, 320, 0.5, 1, false)]
    public void Authoring_ValidatesDepthReferences(double back, double front, double small, double large, bool valid)
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../tests/Fixtures/reference-v1"));
        var room = WorldPackage.Load(root).LoadRooms().First();
        room.DepthScale = new(new(back, small), new(front, large));
        var current = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.5.0"));
        Assert.That(current.Validate(room).IsValid, Is.EqualTo(valid));
        var old = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.4.0"));
        Assert.That(old.Validate(room).IsValid, Is.False);
    }

    [Test]
    public void DepthSettings_SurvivePublicAuthoringAndSnapshotBoundaries()
    {
        var serializer = new RoomYamlSerializer();
        var dto = serializer.Deserialize("""
            version: 1
            id: hill
            name: Hill
            background: assets/images/hill.png
            size: {width: 640, height: 360}
            spawns: [{id: entry, position: {x: 320, y: 300}}]
            walkable: [{id: floor, points: [{x: 0, y: 0}, {x: 640, y: 0}, {x: 640, y: 360}, {x: 0, y: 360}]}]
            depthScale: {back: {y: 120, factor: 0.5}, front: {y: 320, factor: 1}}
            """);
        var validator = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.5.0"));
        Assert.That(validator.Validate(dto).IsValid, Is.True);
        var json = System.Text.Json.JsonSerializer.Serialize(dto.ToRoomDefinition().ToDto(), new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
        Assert.That(json, Does.Contain("\"depthScale\":{\"back\":{\"y\":120,\"factor\":0.5}"));
        Assert.That(serializer.Serialize(RoomYamlDto.FromRoomDefinition(dto.ToRoomDefinition())), Does.Contain("depthScale:"));
    }
}
