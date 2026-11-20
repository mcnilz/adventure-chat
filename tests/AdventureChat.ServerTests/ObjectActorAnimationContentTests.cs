using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

public class ObjectActorAnimationContentTests
{
    [Test]
    public void ActionPerformanceRoundTripsInTheNewFormatOnly()
    {
        var serializer = new RoomYamlSerializer();
        var yaml = serializer.Serialize(DynamicNavigationTests.Room());
        var action = yaml.IndexOf("to: open", StringComparison.Ordinal) + "to: open".Length;
        Assert.That(action, Is.GreaterThan("to: open".Length));
        yaml = yaml.Insert(action, "\n      actorAnimation: { point: 0, direction: up, durationMs: 800 }");
        var room = serializer.Deserialize(yaml);

        Assert.That(new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.10.0")).Validate(room).IsValid, Is.True);
        Assert.That(serializer.Serialize(room), Does.Contain("durationMs: 800"));
        Assert.That(new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.9.0")).Validate(room).IsValid, Is.False);
    }

    [TestCase(1, "up", 800, "existing use point")]
    [TestCase(0, "diagonal", 800, "valid direction")]
    [TestCase(0, "up", 199, "200–5000 ms")]
    public void InvalidAnimationCannotBeLoadedOrSaved(int point, string direction, int duration, string error)
    {
        var room = DynamicNavigationTests.Room();
        room.Objects![0].Interaction!.Actions[0].ActorAnimation = new()
        {
            Point = point, Direction = direction, DurationMs = duration
        };
        var validator = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.10.0"));
        Assert.That(validator.Validate(room).Errors, Has.Some.Contains(error));
    }

    [TestCase("unknown", "workshop-mara-gesture.png", "unknown character")]
    [TestCase("mara", "workshop-board-empty.png", "3 square frame")]
    public void WorldContentRejectsUnknownCharacterIdsAndInvalidSpriteStrips(string character, string image, string error)
    {
        var reference = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../worlds/reference"));
        var world = WorldPackage.Load(reference);
        var workshop = world.LoadRooms().Single(room => room.Id == "workshop");
        var gesture = workshop.Objects!.Single(obj => obj.Id == "board").Interaction!.Actions[0].ActorAnimation!;
        gesture.Sprites = new() { [character] = $"assets/images/{image}" };

        var exception = Assert.Throws<InvalidOperationException>(() => world.ValidateBackground(workshop));
        Assert.That(exception!.Message, Does.Contain(error));
    }
}
