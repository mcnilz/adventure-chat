using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using AdventureChat.Server;
using AdventureChat.Server.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace AdventureChat.ServerTests;

[TestFixture]
public class ApiEndpointTests
{
    private IHost _host = null!;
    private HttpClient _client = null!;
    private string _baseUrl = string.Empty;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var port = GetFreePort();
        _baseUrl = $"http://127.0.0.1:{port}";

        var solutionDir = FindSolutionDirectory();
        var serverDir = Path.Combine(solutionDir, "src", "AdventureChat.Server");

        var app = AppFactory.CreateApp([], new WebApplicationOptions
        {
            ContentRootPath = serverDir,
            WebRootPath = Path.Combine(serverDir, "wwwroot"),
            Args = ["--urls", _baseUrl, "--Content:Root", Path.Combine(solutionDir, "content")]
        });

        _host = app;
        await app.StartAsync();

        _client = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl)
        };
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _client.Dispose();
        await _host.StopAsync();
        _host.Dispose();
    }

    [Test]
    public async Task GetRoom_ExistingRoom_ReturnsRoomDefinitionAndGeometry()
    {
        var response = await _client.GetAsync("/api/rooms/lounge");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await response.Content.ReadFromJsonAsync<RoomResponseDto>();
        Assert.That(json, Is.Not.Null);
        Assert.That(json!.Id, Is.EqualTo("lounge"));
        Assert.That(json.Geometry.OuterBoundary.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(json.Geometry.Obstacles.Count, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task GetRoom_UnknownRoom_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/rooms/dungeon");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task PostPath_ValidPoints_ReturnsWaypoints()
    {
        var request = new PathRequestDto(
            Start: new PointDto(120, 300),
            Target: new PointDto(500, 300)
        );

        var response = await _client.PostAsJsonAsync("/api/rooms/lounge/path", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<PathResponseDto>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Success, Is.True);
        Assert.That(result.Waypoints, Is.Not.Null);
        Assert.That(result.Waypoints!.Count, Is.GreaterThan(2), "Should detour around obstacle.");
    }

    [Test]
    public async Task PostPath_InvalidObstacleTarget_ReturnsBadRequest()
    {
        var request = new PathRequestDto(
            Start: new PointDto(120, 300),
            Target: new PointDto(320, 280) // inside table
        );

        var response = await _client.PostAsJsonAsync("/api/rooms/lounge/path", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var result = await response.Content.ReadFromJsonAsync<PathResponseDto>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Success, Is.False);
        Assert.That(result.Error, Does.Contain("not walkable").IgnoreCase);
    }

    [Test]
    public async Task PostPath_NonFiniteCoordinates_ReturnsBadRequest()
    {
        var rawJson = "{\"start\":{\"x\":120,\"y\":300},\"target\":{\"x\":\"NaN\",\"y\":300}}";
        var content = new StringContent(rawJson, System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/rooms/lounge/path", content);

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

    private sealed record RoomResponseDto(string Id, string Name, RoomGeometryResponseDto Geometry);
    private sealed record RoomGeometryResponseDto(IReadOnlyList<PointDto> OuterBoundary, IReadOnlyList<IReadOnlyList<PointDto>> Obstacles);
    private sealed record PathResponseDto(bool Success, string? Error, IReadOnlyList<PointDto>? Waypoints);
}
