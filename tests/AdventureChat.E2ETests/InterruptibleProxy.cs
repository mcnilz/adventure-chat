using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace AdventureChat.E2ETests;

internal sealed class InterruptibleProxy : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly ConcurrentDictionary<TcpClient, byte> _connections = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly int _targetPort;
    private readonly Task _acceptLoop;
    public string BaseUrl { get; }

    public InterruptibleProxy(string targetUrl)
    {
        _targetPort = new Uri(targetUrl).Port;
        _listener.Start();
        BaseUrl = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}";
        _acceptLoop = AcceptAsync();
    }

    private async Task AcceptAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(_stop.Token);
                var server = new TcpClient();
                _connections.TryAdd(client, 0);
                _connections.TryAdd(server, 0);
                _ = ForwardAsync(client, server);
            }
        }
        catch (OperationCanceledException) { }
        catch (SocketException) when (_stop.IsCancellationRequested) { }
    }

    private async Task ForwardAsync(TcpClient client, TcpClient server)
    {
        try
        {
            await server.ConnectAsync(IPAddress.Loopback, _targetPort, _stop.Token);
            var upstream = client.GetStream().CopyToAsync(server.GetStream(), _stop.Token);
            var downstream = server.GetStream().CopyToAsync(client.GetStream(), _stop.Token);
            await Task.WhenAny(upstream, downstream);
        }
        catch (Exception ex) when (ex is IOException or SocketException or OperationCanceledException or ObjectDisposedException) { }
        finally
        {
            _connections.TryRemove(client, out _);
            _connections.TryRemove(server, out _);
            client.Dispose();
            server.Dispose();
        }
    }

    public void Interrupt()
    {
        foreach (var client in _connections.Keys) client.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        _listener.Stop();
        Interrupt();
        await _acceptLoop;
        _stop.Dispose();
    }
}
