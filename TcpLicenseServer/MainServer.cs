using System.Net.Sockets;
using System.Text;
using TcpLicenseServer.Commands;
using TcpLicenseServer.Models;

namespace TcpLicenseServer;

public class MainServer(CommandFactory commandFactory, int port) : IAsyncDisposable
{
    private TcpListener? _listener;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _listener = TcpListener.Create(port);
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
            Console.WriteLine("The operation was cancelled!");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Critical socket error: {ex}");
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

                if (commandFactory.GetCommand(cmdName) is not null and ICommand command)
                {
                    await command.ExecuteAsync(session, args, ct).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }
        finally
        {
            Console.WriteLine("The client has been disconnected.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _listener?.Stop();

        await ValueTask.CompletedTask;
    }
}
