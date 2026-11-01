using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

[TestFixture]
public class RoomYamlSerializerTests
{
    private RoomYamlSerializer _serializer = null!;

    [SetUp]
    public void SetUp()
    {
        _serializer = new RoomYamlSerializer();
    }

    [Test]
    public void SerializeAndDeserialize_Roundtrip_PreservesDataAndNotes()
    {
        var dto = new RoomYamlDto
        {
            Version = 1,
            Id = "test-room",
            Name = "Test Room",
            Background = "assets/images/test.png",
            Size = new RoomSizeYamlDto { Width = 640, Height = 360 },
            Spawns = [
                new RoomSpawnYamlDto
                {
                    Id = "entry",
                    Position = new RoomPointYamlDto { X = 100, Y = 200 }
                }
            ],
            Walkable = [
                new RoomPolygonYamlDto
                {
                    Id = "floor",
                    Points = [
                        new RoomPointYamlDto { X = 50, Y = 150 },
                        new RoomPointYamlDto { X = 550, Y = 150 },
                        new RoomPointYamlDto { X = 550, Y = 320 },
                        new RoomPointYamlDto { X = 50, Y = 320 }
                    ]
                }
            ],
            Obstacles = [
                new RoomPolygonYamlDto
                {
                    Id = "rock",
                    Points = [
                        new RoomPointYamlDto { X = 200, Y = 200 },
                        new RoomPointYamlDto { X = 250, Y = 200 },
                        new RoomPointYamlDto { X = 250, Y = 250 },
                        new RoomPointYamlDto { X = 200, Y = 250 }
                    ]
                }
            ],
            Exits = [
                new RoomExitYamlDto
                {
                    Id = "to-lounge",
                    Position = new RoomPointYamlDto { X = 520, Y = 200 },
                    Radius = 40,
                    Label = "Zur Lounge",
                    Target = new RoomExitTargetYamlDto { Room = "lounge", Spawn = "entry" }
                }
            ],
            Notes = "Custom authored notes for test room."
        };

        var yaml = _serializer.Serialize(dto);

        Assert.That(yaml, Does.StartWith("# yaml-language-server: $schema="));
        Assert.That(yaml, Does.Contain("test-room"));
        Assert.That(yaml, Does.Contain("Custom authored notes for test room."));

        var deserialized = _serializer.Deserialize(yaml);

        Assert.That(deserialized.Version, Is.EqualTo(1));
        Assert.That(deserialized.Id, Is.EqualTo("test-room"));
        Assert.That(deserialized.Name, Is.EqualTo("Test Room"));
        Assert.That(deserialized.Size.Width, Is.EqualTo(640));
        Assert.That(deserialized.Spawns.Count, Is.EqualTo(1));
        Assert.That(deserialized.Walkable.Count, Is.EqualTo(1));
        Assert.That(deserialized.Walkable[0].Points.Count, Is.EqualTo(4));
        Assert.That(deserialized.Obstacles?.Count, Is.EqualTo(1));
        Assert.That(deserialized.Exits?.Count, Is.EqualTo(1));
        Assert.That(deserialized.Notes, Is.EqualTo("Custom authored notes for test room."));
    }

    [Test]
    public void Deserialize_DuplicateKeys_ThrowsYamlException()
    {
        var duplicateKeyYaml = @"
version: 1
id: room1
id: duplicate_room1
name: Test
background: bg.png
size:
  width: 640
  height: 360
spawns:
  - id: entry
    position: { x: 100, y: 100 }
walkable:
  - id: floor
    points:
      - { x: 0, y: 0 }
      - { x: 100, y: 0 }
      - { x: 100, y: 100 }
";

        Assert.Throws<YamlDotNet.Core.YamlException>(() =>
        {
            _serializer.Deserialize(duplicateKeyYaml);
        });
    }
}
