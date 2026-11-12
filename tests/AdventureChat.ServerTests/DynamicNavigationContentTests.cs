using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

public class DynamicNavigationContentTests
{
    [Test]
    public void StateGeometry_Requires180AndRoundtripsWithoutChangingOldSchemas()
    {
        var room = DynamicNavigationTests.Room();
        var validator = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.8.0"));
        Assert.That(validator.Validate(room).IsValid, Is.True);
        Assert.That(new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.7.0")).Validate(room).IsValid, Is.False);
        var serializer = new RoomYamlSerializer();
        var copy = serializer.Deserialize(serializer.Serialize(RoomYamlDto.FromRoomDefinition(room.ToRoomDefinition())));
        Assert.That(copy.Objects![0].Interaction!.States[0].Obstacles![0].Points[1].X, Is.EqualTo(320));
    }

    [TestCase("spawn")]
    [TestCase("crossed")]
    [TestCase("touching")]
    [TestCase("outside")]
    [TestCase("missing-exit")]
    [TestCase("limits")]
    public void InvalidStateGeometry_IsRejected(string problem)
    {
        var room = DynamicNavigationTests.Room();
        var state = room.Objects![0].Interaction!.States[0];
        switch (problem)
        {
            case "spawn": room.Spawns[0].Position = new() { X = 300, Y = 150 }; break;
            case "crossed": state.Obstacles![0].Points = [new() { X = 20, Y = 20 }, new() { X = 200, Y = 200 }, new() { X = 20, Y = 200 }, new() { X = 180, Y = 20 }]; break;
            case "touching": state.Obstacles![0].Points = [new() { X = 20, Y = 20 }, new() { X = 200, Y = 20 }, new() { X = 200, Y = 200 }, new() { X = 100, Y = 20 }, new() { X = 20, Y = 200 }]; break;
            case "outside": state.Obstacles![0].Points[0].X = -1; break;
            case "missing-exit": state.BlockedExits = ["missing"]; break;
            case "limits": state.Obstacles = Enumerable.Range(0, 5).Select(i => new RoomPolygonYamlDto { Id = i.ToString(), Points = state.Obstacles![0].Points }).ToList(); break;
        }
        var result = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.8.0")).Validate(room);
        Assert.That(result.IsValid, Is.False, string.Join("; ", result.Errors));
    }

    [Test]
    public void EnabledExitBehindInitialObstacle_IsRejectedUnlessExplicitlyBlocked()
    {
        var room = DynamicNavigationTests.Room();
        room.Exits = [new() { Id = "next", Position = new() { X = 500, Y = 150 }, Radius = 30, Target = new() { Room = "dynamic", Spawn = "entry" } }];
        var validator = new RoomValidator(WorldPackage.FindSchema("room.schema.json", "1.8.0"));
        Assert.That(validator.Validate(room).IsValid, Is.False);
        room.Objects![0].Interaction!.States[0].BlockedExits = ["next"];
        Assert.That(validator.Validate(room).IsValid, Is.True);
    }
}
