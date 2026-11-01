using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

[TestFixture]
public class RoomValidatorTests
{
    private RoomValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "schemas", "room.schema.json");
        if (!File.Exists(schemaPath))
        {
            var current = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(current))
            {
                var f = Path.Combine(current, "schemas", "room.schema.json");
                if (File.Exists(f))
                {
                    schemaPath = f;
                    break;
                }
                current = Directory.GetParent(current)?.FullName;
            }
        }

        _validator = new RoomValidator(schemaPath);
    }

    [Test]
    public void Validate_ValidRoom_ReturnsSuccess()
    {
        var dto = CreateValidRoom();
        var result = _validator.Validate(dto);

        Assert.That(result.IsValid, Is.True, string.Join("; ", result.Errors));
    }

    [Test]
    public void Validate_MissingSpawns_ReturnsFailure()
    {
        var dto = CreateValidRoom();
        dto.Spawns.Clear();

        var result = _validator.Validate(dto);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Does.Contain("Room must define at least one spawn point."));
    }

    [Test]
    public void Validate_SpawnOutsideWalkableArea_ReturnsFailure()
    {
        var dto = CreateValidRoom();
        dto.Spawns = [
            new RoomSpawnYamlDto
            {
                Id = "out-of-bounds",
                Position = new RoomPointYamlDto { X = 10, Y = 10 } // outside floor (floor starts at X=40, Y=240)
            }
        ];

        var result = _validator.Validate(dto);

        Assert.That(result.IsValid, Is.False);
        Assert.That(string.Join("; ", result.Errors), Does.Contain("not located on a walkable area"));
    }

    [Test]
    public void Validate_SpawnInsideObstacle_ReturnsFailure()
    {
        var dto = CreateValidRoom();
        dto.Spawns = [
            new RoomSpawnYamlDto
            {
                Id = "in-table",
                Position = new RoomPointYamlDto { X = 300, Y = 280 } // inside obstacle
            }
        ];

        var result = _validator.Validate(dto);

        Assert.That(result.IsValid, Is.False);
        Assert.That(string.Join("; ", result.Errors), Does.Contain("not located on a walkable area"));
    }

    [Test]
    public void Validate_SelfIntersectingPolygon_ReturnsFailure()
    {
        var dto = CreateValidRoom();
        // Bowtie self-intersecting polygon
        dto.Walkable = [
            new RoomPolygonYamlDto
            {
                Id = "floor",
                Points = [
                    new RoomPointYamlDto { X = 0, Y = 0 },
                    new RoomPointYamlDto { X = 100, Y = 100 },
                    new RoomPointYamlDto { X = 100, Y = 0 },
                    new RoomPointYamlDto { X = 0, Y = 100 }
                ]
            }
        ];

        var result = _validator.Validate(dto);

        Assert.That(result.IsValid, Is.False);
        Assert.That(string.Join("; ", result.Errors), Does.Contain("self-intersecting"));
    }

    [Test]
    public void Validate_InvalidIdCharacters_ReturnsFailure()
    {
        var dto = CreateValidRoom();
        dto.Id = "bad room/id!*";

        var result = _validator.Validate(dto);

        Assert.That(result.IsValid, Is.False);
        Assert.That(string.Join("; ", result.Errors), Does.Contain("invalid characters"));
    }

    [Test]
    public void ValidateJson_ValidJson_PassesSchema()
    {
        var json = """
        {
          "version": 1,
          "id": "lounge",
          "name": "Lounge",
          "background": "assets/images/lounge.png",
          "size": { "width": 640, "height": 360 },
          "spawns": [
            { "id": "entry", "position": { "x": 120, "y": 300 } }
          ],
          "walkable": [
            {
              "id": "floor",
              "points": [
                { "x": 40, "y": 240 },
                { "x": 600, "y": 240 },
                { "x": 600, "y": 345 },
                { "x": 40, "y": 345 }
              ]
            }
          ]
        }
        """;

        var result = _validator.ValidateJson(json);

        Assert.That(result.IsValid, Is.True, string.Join("; ", result.Errors));
    }

    [Test]
    public void ValidateJson_InvalidFieldType_FailsSchema()
    {
        var json = """
        {
          "version": "not-an-integer",
          "id": "lounge",
          "name": "Lounge",
          "background": "assets/images/lounge.png",
          "size": { "width": 640, "height": 360 },
          "spawns": [
            { "id": "entry", "position": { "x": 120, "y": 300 } }
          ],
          "walkable": [
            {
              "id": "floor",
              "points": [
                { "x": 40, "y": 240 },
                { "x": 600, "y": 240 },
                { "x": 600, "y": 345 },
                { "x": 40, "y": 345 }
              ]
            }
          ]
        }
        """;

        var result = _validator.ValidateJson(json);

        Assert.That(result.IsValid, Is.False);
    }

    private static RoomYamlDto CreateValidRoom()
    {
        return new RoomYamlDto
        {
            Version = 1,
            Id = "valid-room",
            Name = "Valid Room",
            Background = "assets/images/test.png",
            Size = new RoomSizeYamlDto { Width = 640, Height = 360 },
            Spawns = [
                new RoomSpawnYamlDto
                {
                    Id = "entry",
                    Position = new RoomPointYamlDto { X = 120, Y = 300 }
                }
            ],
            Walkable = [
                new RoomPolygonYamlDto
                {
                    Id = "floor",
                    Points = [
                        new RoomPointYamlDto { X = 40, Y = 240 },
                        new RoomPointYamlDto { X = 600, Y = 240 },
                        new RoomPointYamlDto { X = 600, Y = 345 },
                        new RoomPointYamlDto { X = 40, Y = 345 }
                    ]
                }
            ],
            Obstacles = [
                new RoomPolygonYamlDto
                {
                    Id = "table",
                    Points = [
                        new RoomPointYamlDto { X = 250, Y = 255 },
                        new RoomPointYamlDto { X = 390, Y = 255 },
                        new RoomPointYamlDto { X = 390, Y = 310 },
                        new RoomPointYamlDto { X = 250, Y = 310 }
                    ]
                }
            ],
            Notes = "Test notes"
        };
    }
}
