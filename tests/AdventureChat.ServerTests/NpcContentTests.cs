using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

public class NpcContentTests
{
    [TestCase("host", 150, 40, null)]
    [TestCase("missing", 150, 40, "unknown NPC")]
    [TestCase("host", 900, 40, "unreachable")]
    [TestCase("host", 150, 450, "no reachable spawn in the initial state")]
    public void ActionReactionRoundTripsAndValidatesItsLocalTarget(string npc, int x, int startX, string? error)
    {
        var serializer = new RoomYamlSerializer();
        var yaml = System.Text.RegularExpressions.Regex.Replace(serializer.Serialize(DynamicNavigationTests.Room()),
            @"(?m)^( +)to: open\r?$",
            $"$0\n$1npcReaction: {{ npc: {npc}, position: {{ x: {x}, y: 50 }} }}") +
            NpcYaml.Replace("x: 40, y: 50", $"x: {startX}, y: 50", StringComparison.Ordinal)
                .Replace("x: 60, y: 50", $"x: {startX + 20}, y: 50", StringComparison.Ordinal);
        Assert.That(yaml, Does.Contain("npcReaction:"));
        var room = serializer.Deserialize(yaml);
        var validator = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.15.0"));
        var result = validator.Validate(room);
        if (error is not null)
        {
            Assert.That(result.Errors, Has.Some.Contains(error));
            return;
        }
        Assert.That(result.Errors, Is.Empty);
        var saved = serializer.Serialize(RoomYamlDto.FromRoomDefinition(room.ToRoomDefinition()));
        Assert.That(saved, Does.Contain("npcReaction:").And.Contain("npc: host").And.Contain("x: 150"));
        Assert.That(new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.14.0")).Validate(room).Errors,
            Has.Some.Contains("npcReaction"));
    }

    [Test]
    public void ConfiguredTourSurvivesRoomSaveAndRejectsPriorFormat()
    {
        var serializer = new RoomYamlSerializer();
        var yaml = serializer.Serialize(DynamicNavigationTests.Room()) + NpcYaml.Replace("    options:", """
                followDistance: 48
                tour:
                  - { position: { x: 65, y: 50 }, text: Start }
                  - { exit: next }
                  - { position: { x: 70, y: 50 }, text: Ziel }
                options:
            """, StringComparison.Ordinal);
        var room = serializer.Deserialize(yaml);
        var saved = serializer.Serialize(RoomYamlDto.FromRoomDefinition(room.ToRoomDefinition()));
        Assert.That(saved, Does.Contain("exit: next").And.Contain("text: Ziel").And.Contain("followDistance: 48"));
        Assert.That(new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.14.0")).Validate(room).Errors, Is.Empty);
        Assert.That(new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.13.0")).Validate(room).Errors,
            Has.Some.Contains("tour"));
    }

    private const string NpcYaml = """

            npcs:
              - id: host
                name: Gastgeber
                characterId: mara
                position: { x: 40, y: 50 }
                talkPoint: { x: 60, y: 50 }
                options:
                  - { id: hello, text: Hallo, reply: Willkommen }
                  - { id: garden, text: Wo ist der Garten?, reply: Nebenan }
            """;

    [TestCase("70", true)]
    [TestCase("900", false)]
    public void WaypointsRoundTripOnlyInFormat113AndMustBeWalkable(string x, bool valid)
    {
        var serializer = new RoomYamlSerializer();
        var yaml = serializer.Serialize(DynamicNavigationTests.Room()) + NpcYaml.Replace(
            "    options:", $"    waypoints: [{{ x: 40, y: 50 }}, {{ x: {x}, y: 60 }}]\n    options:",
            StringComparison.Ordinal);
        var room = serializer.Deserialize(yaml);
        var result = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.13.0")).Validate(room);
        Assert.That(result.IsValid, Is.EqualTo(valid), string.Join(Environment.NewLine, result.Errors));
        if (!valid) return;
        var saved = serializer.Serialize(RoomYamlDto.FromRoomDefinition(room.ToRoomDefinition()));
        Assert.That(saved, Does.Contain("waypoints:"));
        Assert.That(new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.12.0")).Validate(room).Errors,
            Has.Some.Contains("waypoints"));
    }

    [Test]
    public void StationaryNpcRoundTripsOnlyInTheNewFormat()
    {
        var serializer = new RoomYamlSerializer();
        var yaml = serializer.Serialize(DynamicNavigationTests.Room()) + NpcYaml;
        var room = serializer.Deserialize(yaml);
        var validator = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.11.0"));

        Assert.That(validator.Validate(room).Errors, Is.Empty);
        var saved = serializer.Serialize(RoomYamlDto.FromRoomDefinition(room.ToRoomDefinition()));
        Assert.That(serializer.Deserialize(saved).Npcs!.Single().Options[0].Reply, Is.EqualTo("Willkommen"));
        Assert.That(new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.10.0")).Validate(room).Errors,
            Has.Some.Contains("npcs"));
    }

    [TestCase("id: garden", "id: HELLO", "unique dialogue option IDs")]
    [TestCase("reply: Nebenan", "reply: ' '", "each with a reply")]
    [TestCase("x: 60, y: 50", "x: 95, y: 50", "walkable talkPoint")]
    public void InvalidDialogueFailsContentValidation(string original, string replacement, string expected)
    {
        var serializer = new RoomYamlSerializer();
        var room = serializer.Deserialize(serializer.Serialize(DynamicNavigationTests.Room()) +
            NpcYaml.Replace(original, replacement, StringComparison.Ordinal));
        var validator = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.11.0"));
        Assert.That(validator.Validate(room).Errors, Has.Some.Contains(expected));
    }

    [Test]
    public void NpcCharacterMustExistInTheWorldCatalog()
    {
        var reference = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../worlds/reference"));
        var world = WorldPackage.Load(reference);
        var lounge = world.LoadRooms().Single(room => room.Id == "lounge");
        lounge.Npcs![0] = lounge.Npcs[0] with { CharacterId = "wrong-character" };

        var exception = Assert.Throws<InvalidOperationException>(() => world.ValidateBackground(lounge));
        Assert.That(exception!.Message, Does.Contain("NPC 'nela' references unknown character 'wrong-character'"));
    }

    [TestCase("door", "closed", true)]
    [TestCase("table", "closed", false)]
    [TestCase("door", "missing", false)]
    public void DialogueConditionsReferenceAnInteractiveObjectAndItsState(string objectId, string state, bool valid)
    {
        var serializer = new RoomYamlSerializer();
        var condition = $"when: {{ object: {objectId}, state: {state} }}";
        var yaml = serializer.Serialize(DynamicNavigationTests.Room()) + NpcYaml.Replace(
            "{ id: hello, text: Hallo, reply: Willkommen }",
            $"{{ id: hello, text: Hallo, reply: Willkommen, {condition} }}", StringComparison.Ordinal);
        var validator = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.12.0"));
        var result = validator.Validate(serializer.Deserialize(yaml));

        Assert.That(result.IsValid, Is.EqualTo(valid), string.Join(Environment.NewLine, result.Errors));
        if (valid)
            Assert.That(serializer.Serialize(serializer.Deserialize(yaml)), Does.Contain("object: door"));
    }
}
