using System.Net;
using System.Net.Sockets;
using AdventureChat.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace AdventureChat.E2ETests;

public sealed class TestServerFixture : IAsyncDisposable
{
    private IHost? _host;
    private readonly bool _authoring;
    private readonly bool _externalWorld;
    private readonly bool _multipleWorlds;
    private readonly bool _mapWorld;
    private readonly bool _scrollWorld;
    public string TempRoot { get; } = Path.Combine(Path.GetTempPath(), "adventure_e2e_" + Guid.NewGuid().ToString("N"));
    public string BaseUrl { get; private set; } = string.Empty;

    public TestServerFixture(bool authoring = false, bool externalWorld = false, bool multipleWorlds = false, bool mapWorld = false, bool scrollWorld = false) { _authoring = authoring; _externalWorld = externalWorld || multipleWorlds || mapWorld || scrollWorld; _mapWorld = mapWorld; _scrollWorld = scrollWorld; _multipleWorlds = multipleWorlds; }

    public async Task StartAsync()
    {
        var port = GetFreePort();
        BaseUrl = $"http://127.0.0.1:{port}";

        var solutionDir = FindSolutionDirectory();
        var serverDir = Path.Combine(solutionDir, "src", "AdventureChat.Server");
        var contentRoot = Path.Combine(TempRoot, "content");
        var webRoot = Path.Combine(TempRoot, "wwwroot");
        if (!Directory.Exists(contentRoot)) CopyDirectory(Path.Combine(solutionDir, "content"), contentRoot);
        if (!Directory.Exists(webRoot)) CopyDirectory(Path.Combine(serverDir, "wwwroot"), webRoot);
        var args = new List<string> { "--urls", BaseUrl, "--Content:Root", contentRoot, "--Content:WebRoot", webRoot };
        if (_externalWorld)
        {
            var worldRoot = Path.Combine(TempRoot, "world");
            if (!Directory.Exists(worldRoot))
            {
                CopyDirectory(Path.Combine(solutionDir, "worlds", _scrollWorld ? "scroll" : _mapWorld ? "map" : "reference"), worldRoot);
                var manifest = Path.Combine(worldRoot, "world.yaml");
                if (!_mapWorld && !_scrollWorld) File.WriteAllText(manifest, File.ReadAllText(manifest).Replace("startRoom: lounge", "startRoom: garden").Replace("startSpawn: entry", "startSpawn: from-lounge"));
            }
            args.AddRange(["--World:Root", worldRoot]);
            if (_multipleWorlds)
            {
                var other = Path.Combine(TempRoot, "other");
                if (!Directory.Exists(other))
                {
                    CopyDirectory(worldRoot, other);
                    var manifest = Path.Combine(other, "world.yaml");
                    File.WriteAllText(manifest, File.ReadAllText(manifest).Replace("id: reference", "id: other"));
                    File.Copy(Path.Combine(other, "assets", "images", "lounge.png"), Path.Combine(other, "assets", "images", "garden.png"), true);
                }
                args.AddRange(["--World:Roots:0", other]);
            }
        }
        if (_authoring) args.Add("--authoring");

        var app = AppFactory.CreateApp([], new WebApplicationOptions
        {
            ContentRootPath = serverDir,
            WebRootPath = webRoot,
            Args = args.ToArray()
        });

        _host = app;
        await app.StartAsync();
    }

    public async Task StopAsync()
    {
        if (_host is null) return;
        await _host.StopAsync();
        _host.Dispose();
        _host = null;
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        foreach (var dir in Directory.GetDirectories(source)) CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
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

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        if (Directory.Exists(TempRoot)) Directory.Delete(TempRoot, true);
    }
}
