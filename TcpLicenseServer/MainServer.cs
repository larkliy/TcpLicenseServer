using Serilog;
using System.Net.Sockets;
using System.Text;
using TcpLicenseServer.Commands;
using TcpLicenseServer.Models;

namespace TcpLicenseServer;

public class MainServer : IAsyncDisposable
{
    private TcpListener? _listener;
    private readonly CommandFactory _commandFactory;
    private readonly SessionRegistry _sessionRegistry;
    private readonly int _port;

    public MainServer(SessionRegistry sessionRegistry,
                      CommandFactory commandFactory,
                      int port)
    {
        _sessionRegistry = sessionRegistry;
        _commandFactory = commandFactory;
        _port = port;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _listener = TcpListener.Create(_port);
        _listener.Start();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);

                _ = ProcessClientAsync(client, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            Log.Information("The operation was cancelled!");
        }
        catch (SocketException ex)
        {
            Log.Warning("Critical socket error: {Exception}", ex);
            throw;
        }
        finally
        {
            _listener?.Dispose();
        }
    }

    private async Task ProcessClientAsync(TcpClient client, CancellationToken ct)
    {
        var session = new ClientSession(client);
        using var reader = new StreamReader(session.Stream, Encoding.UTF8, leaveOpen: true);

        using var _ = client;
        client.NoDelay = true;

        try
        {
            await session.SendAsync("WELCOME: Service v1.0. Type 'LOGIN admin secret123'.", ct);

            string? line;
            while ((line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string cmdName = parts[0];

                string[] args = parts.Length > 1 ? parts[1..] : [];

                if (_commandFactory.GetCommand(cmdName) is not null and ICommand command)
                {
                    Log.Information("The \"{CommandName}\" command has been called. With args: {args}", command.GetType().FullName, args);

                    await command.ExecuteAsync(_sessionRegistry, session, args, ct).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }
        finally
        {
            if (session.Userkey is not null)
                _sessionRegistry.Remove(session.Userkey);

            Log.Information("The client has been disconnected.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _listener?.Stop();

        await ValueTask.CompletedTask;
    }
}
