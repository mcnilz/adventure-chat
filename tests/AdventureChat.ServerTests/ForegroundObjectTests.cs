using AdventureChat.Server.Services;
using AdventureChat.Server.Models;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

public class ForegroundObjectTests
{
    private static string Fixture => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../tests/Fixtures/foreground"));

    [Test]
    public void Package_LoadsObjectsWithoutChangingNavigation()
    {
        var world = WorldPackage.Load(Fixture);
        var room = world.LoadRooms().Single(r => r.Id == "hall").ToRoomDefinition();
        Assert.That(room.Objects!.Single().DepthY, Is.EqualTo(280));
        Assert.That(new PathfindingService().CalculatePath(room.Geometry, new(230,260), new(230,310)).Success, Is.True);
        var runtime = new RoomService(world: world).GetRoom("hall")!;
        Assert.That(runtime.Objects!.Single().Image, Is.EqualTo("/world-assets/foreground/images/table.png"));
        Assert.That(runtime.ToDto().Objects, Is.EqualTo(runtime.Objects));
    }

    [TestCase("id: table", "id: 'bad id'")]
    [TestCase("depthY: 280", "depthY: -1")]
    [TestCase("depthY: 280", "depthY: 361")]
    [TestCase("depthY: 280", "depthY: 280.5")]
    [TestCase("width: 120", "width: 0")]
    [TestCase("width: 120", "width: 999")]
    [TestCase("x: 200", "x: -1")]
    [TestCase("table.png", "missing.png")]
    [TestCase("assets/images/table.png", "assets/images/../table.png")]
    [TestCase("assets/images/table.png", "C:/table.png")]
    public void Package_RejectsInvalidObjects(string before, string after)
    {
        var root = Path.Combine(Path.GetTempPath(), "object-world-"+Guid.NewGuid().ToString("N"));
        try
        {
            foreach (var file in Directory.GetFiles(Fixture, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(root, Path.GetRelativePath(Fixture,file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(file,target);
            }
            var path = Path.Combine(root,"rooms/hall.yaml");
            File.WriteAllText(path, File.ReadAllText(path).Replace(before,after));
            Assert.Throws<InvalidOperationException>(() => WorldPackage.Load(root).LoadRooms());
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root,true); }
    }

    [Test]
    public void DuplicateIdsAndExcessObjects_AreRejectedAtAuthoringBoundary()
    {
        var room = WorldPackage.Load(Fixture).LoadRooms().Single(r=>r.Id=="hall");
        var validator = new RoomValidator(WorldPackage.FindSchema("room.schema.json","1.4.0"));
        room.Objects!.Add(room.Objects[0]);
        Assert.That(validator.Validate(room).IsValid,Is.False);
        room.Objects = Enumerable.Range(0,129).Select(i=>new AdventureChat.Server.Models.RoomObjectYamlDto {
            Id="object-"+i, Image="assets/images/table.png", Position=new(){X=0,Y=0},
            Size=new(){Width=10,Height=10}, DepthY=10 }).ToList();
        Assert.That(validator.Validate(room).IsValid,Is.False);
    }
    [Test]
    public void ObjectData_SurvivesAuthoringRoundtrip()
    {
        const string yaml = """
            version: 1
            id: hall
            name: Hall
            background: assets/images/bg.png
            size: { width: 640, height: 360 }
            spawns: [{ id: entry, position: { x: 100, y: 300 } }]
            walkable: [{ id: floor, points: [{x: 0, y: 0}, {x: 640, y: 0}, {x: 640, y: 360}, {x: 0, y: 360}] }]
            objects:
              - { id: table, image: assets/images/table.png, position: {x: 200, y: 200}, size: {width: 100, height: 100}, depthY: 280 }
            """;
        var serializer = new RoomYamlSerializer();
        var dto = serializer.Deserialize(yaml);
        var result = serializer.Serialize(AdventureChat.Server.Models.RoomYamlDto.FromRoomDefinition(dto.ToRoomDefinition()));
        Assert.That(result, Does.Contain("table.png").And.Contain("depthY: 280"));
    }
    [Test]
    public void Objects_AreAcceptedOnlyByNewFormat()
    {
        var json = """
            {"version":1,"id":"hall","name":"Hall","background":"assets/images/bg.png",
             "size":{"width":640,"height":360},"spawns":[{"id":"entry","position":{"x":100,"y":300}}],
             "walkable":[{"id":"floor","points":[{"x":0,"y":0},{"x":640,"y":0},{"x":640,"y":360},{"x":0,"y":360}]}],
             "objects":[{"id":"table","image":"assets/images/table.png","position":{"x":200,"y":200},"size":{"width":100,"height":100},"depthY":280}]}
            """;
        var validator = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.4.0"));
        Assert.That(validator.ValidateJson(json).IsValid, Is.True);
        var old = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.3.0"));
        Assert.That(old.ValidateJson(json).IsValid, Is.False);
    }
}
