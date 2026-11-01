using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using AdventureChat.Server;
using AdventureChat.Server.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

[TestFixture]
public class EditorEndpointTests
{
    private IHost _host = null!;
    private HttpClient _client = null!;
    private string _baseUrl = string.Empty;
    private string _tempContentDir = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _tempContentDir = Path.Combine(Path.GetTempPath(), "adventure_chat_editor_api_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_tempContentDir, "rooms"));

        var port = GetFreePort();
        _baseUrl = $"http://127.0.0.1:{port}";

        var solutionDir = FindSolutionDirectory();
        var serverDir = Path.Combine(solutionDir, "src", "AdventureChat.Server");

        var app = AppFactory.CreateApp([], new WebApplicationOptions
        {
            ContentRootPath = serverDir,
            WebRootPath = Path.Combine(serverDir, "wwwroot"),
            Args = ["--urls", _baseUrl]
        });

        _host = app;
        await app.StartAsync();

        _client = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl)
        };
    }

    private readonly List<string> _createdFiles = new();

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        foreach (var file in _createdFiles)
        {
            if (File.Exists(file))
            {
                try { File.Delete(file); } catch { /* ignore */ }
            }
        }

        _client.Dispose();
        await _host.StopAsync();
        _host.Dispose();

        if (Directory.Exists(_tempContentDir))
        {
            try { Directory.Delete(_tempContentDir, recursive: true); } catch { /* ignore */ }
        }
    }

    [Test]
    public async Task GetSchema_ReturnsJsonSchema()
    {
        var response = await _client.GetAsync("/schemas/room.schema.json");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Does.Contain("RoomDefinition"));
        Assert.That(content, Does.Contain("walkable"));
    }

    [Test]
    public async Task GetEditorStatus_ReturnsEnabledAndSchemaUrl()
    {
        var response = await _client.GetAsync("/api/editor/status");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(json.GetProperty("enabled").GetBoolean(), Is.True);
        Assert.That(json.GetProperty("schemaUrl").GetString(), Is.EqualTo("/schemas/room.schema.json"));
    }

    [Test]
    public async Task GetEditorRooms_ReturnsRoomSummaries()
    {
        var response = await _client.GetAsync("/api/editor/rooms");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var rooms = await response.Content.ReadFromJsonAsync<List<RoomSummaryResponseDto>>();
        Assert.That(rooms, Is.Not.Null);
        Assert.That(rooms!.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(rooms.Any(r => r.Id == "lounge"), Is.True);
    }

    [Test]
    public async Task ValidateRoom_ValidRoom_ReturnsValidTrue()
    {
        var dto = new RoomYamlDto
        {
            Version = 1,
            Id = "test-validation",
            Name = "Validation Test",
            Background = "bg.png",
            Size = new RoomSizeYamlDto { Width = 640, Height = 360 },
            Spawns = [new RoomSpawnYamlDto { Id = "entry", Position = new RoomPointYamlDto { X = 100, Y = 200 } }],
            Walkable = [
                new RoomPolygonYamlDto
                {
                    Id = "floor",
                    Points = [
                        new RoomPointYamlDto { X = 40, Y = 100 },
                        new RoomPointYamlDto { X = 500, Y = 100 },
                        new RoomPointYamlDto { X = 500, Y = 300 },
                        new RoomPointYamlDto { X = 40, Y = 300 }
                    ]
                }
            ]
        };

        var response = await _client.PostAsJsonAsync("/api/editor/validate", dto);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await response.Content.ReadFromJsonAsync<ValidationResponseDto>();
        Assert.That(json, Is.Not.Null);
        Assert.That(json!.Valid, Is.True);
        Assert.That(json.Errors, Is.Empty);
    }

    [Test]
    public async Task SaveRoom_ValidRoom_ReturnsOkAndPersists()
    {
        var dto = new RoomYamlDto
        {
            Version = 1,
            Id = "custom-created-room",
            Name = "Custom Created Room",
            Background = "assets/images/lounge.png",
            Size = new RoomSizeYamlDto { Width = 640, Height = 360 },
            Spawns = [new RoomSpawnYamlDto { Id = "entry", Position = new RoomPointYamlDto { X = 100, Y = 200 } }],
            Walkable = [
                new RoomPolygonYamlDto
                {
                    Id = "floor",
                    Points = [
                        new RoomPointYamlDto { X = 40, Y = 100 },
                        new RoomPointYamlDto { X = 500, Y = 100 },
                        new RoomPointYamlDto { X = 500, Y = 300 },
                        new RoomPointYamlDto { X = 40, Y = 300 }
                    ]
                }
            ],
            Notes = "Authored room notes"
        };

        var response = await _client.PostAsJsonAsync("/api/editor/rooms/custom-created-room", dto);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<SaveRoomResponseDto>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Success, Is.True);
        Assert.That(result.RoomId, Is.EqualTo("custom-created-room"));

        // Fetch back via regular runtime endpoint
        var runtimeResponse = await _client.GetAsync("/api/rooms/custom-created-room");
        Assert.That(runtimeResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var solutionDir = FindSolutionDirectory();
        var targetFile = Path.Combine(solutionDir, "content", "rooms", "custom-created-room.yaml");
        _createdFiles.Add(targetFile);
    }

    [Test]
    public async Task SaveRoom_InvalidRoomGeometry_ReturnsBadRequest()
    {
        var brokenDto = new RoomYamlDto
        {
            Version = 1,
            Id = "broken-room",
            Name = "Broken Room",
            Background = "bg.png",
            Size = new RoomSizeYamlDto { Width = 640, Height = 360 },
            Spawns = [new RoomSpawnYamlDto { Id = "entry", Position = new RoomPointYamlDto { X = 9999, Y = 9999 } }], // outside walkable
            Walkable = [
                new RoomPolygonYamlDto
                {
                    Id = "floor",
                    Points = [
                        new RoomPointYamlDto { X = 40, Y = 100 },
                        new RoomPointYamlDto { X = 500, Y = 100 },
                        new RoomPointYamlDto { X = 500, Y = 300 },
                        new RoomPointYamlDto { X = 40, Y = 300 }
                    ]
                }
            ]
        };

        var response = await _client.PostAsJsonAsync("/api/editor/rooms/broken-room", brokenDto);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task SaveRoom_PathMismatch_ReturnsBadRequest()
    {
        var dto = new RoomYamlDto
        {
            Version = 1,
            Id = "room-a",
            Name = "Room A",
            Background = "bg.png",
            Size = new RoomSizeYamlDto { Width = 640, Height = 360 },
            Spawns = [new RoomSpawnYamlDto { Id = "entry", Position = new RoomPointYamlDto { X = 100, Y = 200 } }],
            Walkable = [
                new RoomPolygonYamlDto
                {
                    Id = "floor",
                    Points = [
                        new RoomPointYamlDto { X = 40, Y = 100 },
                        new RoomPointYamlDto { X = 500, Y = 100 },
                        new RoomPointYamlDto { X = 500, Y = 300 },
                        new RoomPointYamlDto { X = 40, Y = 300 }
                    ]
                }
            ]
        };

        var response = await _client.PostAsJsonAsync("/api/editor/rooms/room-b", dto);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string FindSolutionDirectory()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "AdventureChat.sln")) ||
                File.Exists(Path.Combine(current, "AdventureChat.slnx")) ||
                File.Exists(Path.Combine(current, "SPEC.md")))
            {
                return current;
            }
            current = Directory.GetParent(current)?.FullName;
        }
        throw new InvalidOperationException("Could not find solution directory.");
    }

    private sealed record RoomSummaryResponseDto(string Id, string Name, string Background, int Width, int Height);
    private sealed record ValidationResponseDto(bool Valid, IReadOnlyList<string> Errors);
    private sealed record SaveRoomResponseDto(bool Success, string? RoomId, IReadOnlyList<string>? Errors);
}
