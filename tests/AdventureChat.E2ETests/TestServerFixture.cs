using System.Net;
using System.Net.Sockets;
using AdventureChat.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace AdventureChat.E2ETests;

public sealed class TestServerFixture : IAsyncDisposable
{
    private IHost? _host;
    public string BaseUrl { get; private set; } = string.Empty;

    public async Task StartAsync()
    {
        var port = GetFreePort();
        BaseUrl = $"http://127.0.0.1:{port}";

        var solutionDir = FindSolutionDirectory();
        var serverDir = Path.Combine(solutionDir, "src", "AdventureChat.Server");

        var app = AppFactory.CreateApp([], new WebApplicationOptions
        {
            ContentRootPath = serverDir,
            WebRootPath = Path.Combine(serverDir, "wwwroot"),
            Args = ["--urls", BaseUrl]
        });

        _host = app;
        await app.StartAsync();
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
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
