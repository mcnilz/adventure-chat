using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

public class ObjectTransitionContentTests
{
    [Test]
    public void NewFormatLoadsAndRoundtripsDirectionalTransitions()
    {
        var serializer = new RoomYamlSerializer();
        var yaml = serializer.Serialize(DynamicNavigationTests.Room()).Replace("    actions:", """
                transitions:
                - { from: closed, to: open, durationMs: 900 }
                - { from: open, to: closed, durationMs: 650 }
                actions:
            """);
        Assert.That(yaml, Does.Contain("transitions:"));
        var room = serializer.Deserialize(yaml);
        var validator = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.9.0"));
        Assert.That(validator.Validate(room).IsValid, Is.True);
        Assert.That(serializer.Serialize(room), Does.Contain("durationMs: 900"));
        Assert.That(new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.8.0")).Validate(room).IsValid, Is.False);
    }

    [TestCase("unknown", "open", 900)]
    [TestCase("open", "open", 900)]
    [TestCase("closed", "open", 99)]
    [TestCase("closed", "open", 5001)]
    public void InvalidTransitionsFailContentValidation(string from, string to, int durationMs)
    {
        var room = DynamicNavigationTests.Room();
        room.Objects![0].Interaction!.Transitions = [new ObjectTransition { From = from, To = to, DurationMs = durationMs }];
        var validator = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.9.0"));
        Assert.That(validator.Validate(room).Errors, Has.Some.Contains("transition"));
    }
}
