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
    [Test]
    public void Start_WithInvalidYaml_FailsWithFileDiagnostic()
    {
        var root = Path.Combine(Path.GetTempPath(), "adventure_invalid_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "rooms"));
        File.WriteAllText(Path.Combine(root, "rooms", "broken.yaml"), "version: [invalid\n");
        var serverDir = Path.Combine(FindSolutionDirectory(), "src", "AdventureChat.Server");
        var app = AppFactory.CreateApp([], new WebApplicationOptions
        {
            ContentRootPath = serverDir,
            WebRootPath = Path.Combine(serverDir, "wwwroot"),
            Args = ["--urls", $"http://127.0.0.1:{GetFreePort()}", "--Content:Root", root, "--Content:WebRoot", Path.Combine(root, "web")]
        });
        try
        {
            var error = Assert.ThrowsAsync<InvalidOperationException>(async () => await app.StartAsync());
            Assert.That(error!.Message, Does.Contain("broken.yaml"));
        }
        finally
        {
            app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void Start_WithMissingRequiredVersion_FailsWithFileDiagnostic()
    {
        var root = Path.Combine(Path.GetTempPath(), "adventure_missing_version_" + Guid.NewGuid().ToString("N"));
        var rooms = Path.Combine(root, "rooms");
        Directory.CreateDirectory(rooms);
        var source = Path.Combine(FindSolutionDirectory(), "content", "rooms");
        foreach (var file in Directory.GetFiles(source, "*.yaml"))
        {
            var contents = File.ReadAllText(file);
            if (Path.GetFileName(file) == "lounge.yaml") contents = contents.Replace("version: 1", "");
            File.WriteAllText(Path.Combine(rooms, Path.GetFileName(file)), contents);
        }
        var serverDir = Path.Combine(FindSolutionDirectory(), "src", "AdventureChat.Server");
        var app = AppFactory.CreateApp([], new WebApplicationOptions
        {
            ContentRootPath = serverDir,
            WebRootPath = Path.Combine(serverDir, "wwwroot"),
            Args = ["--urls", $"http://127.0.0.1:{GetFreePort()}", "--Content:Root", root, "--Content:WebRoot", Path.Combine(root, "web")]
        });
        try
        {
            var error = Assert.ThrowsAsync<InvalidOperationException>(async () => await app.StartAsync());
            Assert.That(error!.Message, Does.Contain("lounge.yaml"));
        }
        finally
        {
            app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void Start_WithMissingExitDestination_FailsWithFileDiagnostic()
    {
        var root = Path.Combine(Path.GetTempPath(), "adventure_missing_target_" + Guid.NewGuid().ToString("N"));
        var rooms = Path.Combine(root, "rooms");
        Directory.CreateDirectory(rooms);
        var source = Path.Combine(FindSolutionDirectory(), "content", "rooms");
        foreach (var file in Directory.GetFiles(source, "*.yaml"))
        {
            var contents = File.ReadAllText(file);
            if (Path.GetFileName(file) == "lounge.yaml") contents = contents.Replace("room: garden", "room: missing-room");
            File.WriteAllText(Path.Combine(rooms, Path.GetFileName(file)), contents);
        }
        var serverDir = Path.Combine(FindSolutionDirectory(), "src", "AdventureChat.Server");
        var app = AppFactory.CreateApp([], new WebApplicationOptions
        {
            ContentRootPath = serverDir,
            WebRootPath = Path.Combine(serverDir, "wwwroot"),
            Args = ["--urls", $"http://127.0.0.1:{GetFreePort()}", "--Content:Root", root, "--Content:WebRoot", Path.Combine(root, "web")]
        });
        try
        {
            var error = Assert.ThrowsAsync<InvalidOperationException>(async () => await app.StartAsync());
            Assert.That(error!.Message, Does.Contain("lounge.yaml").And.Contain("missing-room"));
        }
        finally
        {
            app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            Directory.Delete(root, true);
        }
    }
    [Test]
    public async Task DefaultStart_RejectsEditorApi()
    {
        var port = GetFreePort();
        var url = $"http://127.0.0.1:{port}";
        var serverDir = Path.Combine(FindSolutionDirectory(), "src", "AdventureChat.Server");
        var app = AppFactory.CreateApp([], new WebApplicationOptions
        {
            ContentRootPath = serverDir,
            WebRootPath = Path.Combine(serverDir, "wwwroot"),
            Args = ["--urls", url]
        });
        await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(url) };
            var response = await client.GetAsync("/api/editor/status");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            var page = await client.GetAsync("/editor.html");
            Assert.That(page.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
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
        foreach (var file in Directory.GetFiles(Path.Combine(solutionDir, "content", "rooms"), "*.yaml"))
            File.Copy(file, Path.Combine(_tempContentDir, "rooms", Path.GetFileName(file)));
        var testWebRoot = Path.Combine(_tempContentDir, "webroot");
        var images = Path.Combine(testWebRoot, "assets", "images");
        Directory.CreateDirectory(images);
        foreach (var file in Directory.GetFiles(Path.Combine(serverDir, "wwwroot", "assets", "images")))
            File.Copy(file, Path.Combine(images, Path.GetFileName(file)));

        var app = AppFactory.CreateApp([], new WebApplicationOptions
        {
            ContentRootPath = serverDir,
            WebRootPath = Path.Combine(serverDir, "wwwroot"),
            Args = ["--urls", _baseUrl, "--authoring", "--Content:Root", _tempContentDir, "--Content:WebRoot", testWebRoot]
        });

        _host = app;
        await app.StartAsync();

        _client = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl)
        };
        var status = await _client.GetFromJsonAsync<JsonElement>("/api/editor/status");
        _client.DefaultRequestHeaders.Add("X-Author-Token", status.GetProperty("token").GetString());
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
    public async Task EditorApi_RejectsForeignHostAndOrigin()
    {
        using var foreignHost = new HttpRequestMessage(HttpMethod.Get, "/api/editor/status");
        foreignHost.Headers.Host = "evil.example";
        var hostResponse = await _client.SendAsync(foreignHost);
        Assert.That(hostResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        using var foreignOrigin = new HttpRequestMessage(HttpMethod.Get, "/api/editor/status");
        foreignOrigin.Headers.Add("Origin", "https://evil.example");
        var originResponse = await _client.SendAsync(foreignOrigin);
        Assert.That(originResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task EditorWrite_WithoutAuthorToken_IsForbidden()
    {
        using var client = new HttpClient { BaseAddress = new Uri(_baseUrl) };
        var response = await client.PostAsJsonAsync("/api/editor/rooms/test", new { id = "test" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task UploadBackground_InvalidPngSignature_IsRejected()
    {
        var status = await _client.GetFromJsonAsync<JsonElement>("/api/editor/status");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/editor/backgrounds");
        var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent([1, 2, 3, 4]), "file", "invalid-signature.png");
        request.Content = form;
        var response = await _client.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [TestCase("image.svg", false)]
    [TestCase("large.png", true)]
    public async Task UploadBackground_SvgOrOversize_IsRejected(string name, bool large)
    {
        using var form = new MultipartFormDataContent();
        var bytes = new byte[large ? 10 * 1024 * 1024 + 1 : 8];
        new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }.CopyTo(bytes, 0);
        form.Add(new ByteArrayContent(bytes), "file", name);
        var response = await _client.PostAsync("/api/editor/backgrounds", form);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(File.Exists(Path.Combine(_tempContentDir, "webroot", "assets", "images", name)), Is.False);
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

        var targetFile = Path.Combine(_tempContentDir, "rooms", "custom-created-room.yaml");
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

    [Test]
    public async Task ValidateWorld_Get_ReturnsValidForDefaultWorld()
    {
        var response = await _client.GetAsync("/api/editor/validate-world");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<ValidationResponseDto>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Valid, Is.True, string.Join("; ", result.Errors));
    }

    [Test]
    public async Task ValidateWorld_Post_WithBrokenExit_ReturnsValidFalse()
    {
        var brokenRoom = new RoomYamlDto
        {
            Version = 1,
            Id = "world-test-room",
            Name = "World Test",
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
            Exits = [
                new RoomExitYamlDto
                {
                    Id = "to-nowhere",
                    Position = new RoomPointYamlDto { X = 400, Y = 200 },
                    Radius = 25,
                    Target = new RoomExitTargetYamlDto { Room = "non-existent-world-room", Spawn = "entry" }
                }
            ]
        };

        var response = await _client.PostAsJsonAsync("/api/editor/validate-world", new[] { brokenRoom });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<ValidationResponseDto>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Valid, Is.False);
        Assert.That(string.Join("; ", result.Errors), Does.Contain("references non-existent target room 'non-existent-world-room'"));
    }

    [Test]
    public async Task GetBackgrounds_ReturnsOverviewAndExistingImages()
    {
        var response = await _client.GetAsync("/api/editor/backgrounds");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var overview = await response.Content.ReadFromJsonAsync<BackgroundsOverviewResponseDto>();
        Assert.That(overview, Is.Not.Null);
        Assert.That(overview!.FilesystemDirectory, Is.Not.Empty);
        Assert.That(overview.RelativePath, Is.EqualTo("assets/images"));
        Assert.That(overview.Backgrounds, Is.Not.Null);
        Assert.That(overview.Backgrounds.Count, Is.GreaterThanOrEqualTo(5)); // lounge, garden, library, courtyard, cafe
        Assert.That(overview.Backgrounds.Any(b => b.Name == "library.png"), Is.True);
        Assert.That(overview.Backgrounds.Any(b => b.Name == "courtyard.png"), Is.True);
        Assert.That(overview.Backgrounds.Any(b => b.Name == "cafe.png"), Is.True);
    }

    [Test]
    public async Task UploadBackground_ValidPng_SavesAndReturnsPath()
    {
        using var form = new MultipartFormDataContent();
        var fakePngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
        var fileContent = new ByteArrayContent(fakePngBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", "test-uploaded-bg.png");

        var response = await _client.PostAsync("/api/editor/backgrounds", form);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<UploadBackgroundResponseDto>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Success, Is.True);
        Assert.That(result.Name, Is.EqualTo("test-uploaded-bg.png"));
        Assert.That(result.Path, Is.EqualTo("assets/images/test-uploaded-bg.png"));

        var targetFile = Path.Combine(_tempContentDir, "webroot", "assets", "images", "test-uploaded-bg.png");
        _createdFiles.Add(targetFile);
    }

    [Test]
    public async Task UploadBackground_DisallowedExtension_ReturnsBadRequest()
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new StringContent("malicious script");
        form.Add(fileContent, "file", "dangerous.exe");

        var response = await _client.PostAsync("/api/editor/backgrounds", form);

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
    private sealed record BackgroundItemResponseDto(string Name, string Path, long SizeBytes, DateTimeOffset LastModified);
    private sealed record BackgroundsOverviewResponseDto(string FilesystemDirectory, string RelativePath, IReadOnlyList<BackgroundItemResponseDto> Backgrounds);
    private sealed record UploadBackgroundResponseDto(bool Success, string? Name, string? Path, string? Error);
}
