using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

[TestFixture]
public class EditorServiceTests
{
    private string _tempContentDir = null!;
    private IContentPathProvider _pathProvider = null!;
    private IRoomYamlSerializer _serializer = null!;
    private IRoomValidator _validator = null!;
    private IRoomService _roomService = null!;
    private EditorService _editorService = null!;

    [SetUp]
    public void SetUp()
    {
        _tempContentDir = Path.Combine(Path.GetTempPath(), "adventure_chat_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_tempContentDir, "rooms"));

        _pathProvider = new ContentPathProvider(_tempContentDir);
        _serializer = new RoomYamlSerializer();
        _validator = new RoomValidator();
        _roomService = new RoomService(_pathProvider, _serializer);
        _editorService = new EditorService(_pathProvider, _serializer, _validator, _roomService, editorEnabled: true);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempContentDir))
        {
            try { Directory.Delete(_tempContentDir, recursive: true); } catch { /* ignore */ }
        }
    }

    [Test]
    public void SaveRoom_ValidRoom_SavesAtomicallyAndCanBeReloaded()
    {
        var dto = new RoomYamlDto
        {
            Version = 1,
            Id = "tavern-cellar",
            Name = "Tavern Cellar",
            Background = "assets/images/cellar.png",
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
                        new RoomPointYamlDto { X = 50, Y = 100 },
                        new RoomPointYamlDto { X = 550, Y = 100 },
                        new RoomPointYamlDto { X = 550, Y = 300 },
                        new RoomPointYamlDto { X = 50, Y = 300 }
                    ]
                }
            ],
            Notes = "Cellar room"
        };

        var saveResult = _editorService.SaveRoom(dto);

        Assert.That(saveResult.Success, Is.True, string.Join("; ", saveResult.Errors));
        Assert.That(saveResult.RoomId, Is.EqualTo("tavern-cellar"));

        var savedFile = _pathProvider.GetRoomFilePath("tavern-cellar");
        Assert.That(File.Exists(savedFile), Is.True);

        // Verify loaded data
        var loaded = _editorService.GetRoom("tavern-cellar");
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Name, Is.EqualTo("Tavern Cellar"));
        Assert.That(loaded.Notes, Is.EqualTo("Cellar room"));

        // Verify in-memory runtime was updated
        var runtimeRoom = _roomService.GetRoom("tavern-cellar");
        Assert.That(runtimeRoom, Is.Not.Null);
        Assert.That(runtimeRoom!.Name, Is.EqualTo("Tavern Cellar"));
    }

    [Test]
    public void SaveRoom_InvalidRoom_DoesNotOverwriteExistingFile()
    {
        var initialDto = new RoomYamlDto
        {
            Version = 1,
            Id = "safe-room",
            Name = "Original Safe Room",
            Background = "assets/images/safe.png",
            Size = new RoomSizeYamlDto { Width = 640, Height = 360 },
            Spawns = [
                new RoomSpawnYamlDto { Id = "entry", Position = new RoomPointYamlDto { X = 100, Y = 200 } }
            ],
            Walkable = [
                new RoomPolygonYamlDto
                {
                    Id = "floor",
                    Points = [
                        new RoomPointYamlDto { X = 50, Y = 100 },
                        new RoomPointYamlDto { X = 550, Y = 100 },
                        new RoomPointYamlDto { X = 550, Y = 300 },
                        new RoomPointYamlDto { X = 50, Y = 300 }
                    ]
                }
            ]
        };

        var firstSave = _editorService.SaveRoom(initialDto);
        Assert.That(firstSave.Success, Is.True);

        // Now attempt to save an invalid version of the same room (spawn out of bounds)
        var brokenDto = new RoomYamlDto
        {
            Version = 1,
            Id = "safe-room",
            Name = "Corrupted Room",
            Background = "assets/images/safe.png",
            Size = new RoomSizeYamlDto { Width = 640, Height = 360 },
            Spawns = [
                new RoomSpawnYamlDto { Id = "entry", Position = new RoomPointYamlDto { X = 9999, Y = 9999 } }
            ],
            Walkable = [
                new RoomPolygonYamlDto
                {
                    Id = "floor",
                    Points = [
                        new RoomPointYamlDto { X = 50, Y = 100 },
                        new RoomPointYamlDto { X = 550, Y = 100 },
                        new RoomPointYamlDto { X = 550, Y = 300 },
                        new RoomPointYamlDto { X = 50, Y = 300 }
                    ]
                }
            ]
        };

        var secondSave = _editorService.SaveRoom(brokenDto);
        Assert.That(secondSave.Success, Is.False);

        // Verify existing file is completely unchanged
        var current = _editorService.GetRoom("safe-room");
        Assert.That(current, Is.Not.Null);
        Assert.That(current!.Name, Is.EqualTo("Original Safe Room"));
    }

    [Test]
    public void GetRoomFilePath_PathTraversal_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _pathProvider.GetRoomFilePath("../outside");
        });

        Assert.Throws<ArgumentException>(() =>
        {
            _pathProvider.GetRoomFilePath("..\\..\\windows\\system32");
        });
    }

    [Test]
    public void SaveRoom_WhenEditorDisabled_ReturnsFailure()
    {
        var disabledEditor = new EditorService(_pathProvider, _serializer, _validator, _roomService, editorEnabled: false);
        var dto = new RoomYamlDto
        {
            Version = 1,
            Id = "room-disabled",
            Name = "Disabled",
            Background = "bg.png",
            Size = new RoomSizeYamlDto { Width = 640, Height = 360 },
            Spawns = [new RoomSpawnYamlDto { Id = "entry", Position = new RoomPointYamlDto { X = 100, Y = 200 } }],
            Walkable = [new RoomPolygonYamlDto { Id = "floor", Points = [new RoomPointYamlDto { X = 50, Y = 100 }, new RoomPointYamlDto { X = 500, Y = 100 }, new RoomPointYamlDto { X = 500, Y = 300 }, new RoomPointYamlDto { X = 50, Y = 300 }] }]
        };

        var result = disabledEditor.SaveRoom(dto);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors[0], Does.Contain("disabled"));
    }
}
