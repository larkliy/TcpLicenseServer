using System.Net;
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
        _listener = new TcpListener(IPAddress.Any, port);

        _listener.Start();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken)
                    .ConfigureAwait(false);

                _ = ProcessClientAsync(client, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Операция была отменена!");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Критическая ошибка сокета: {ex}");
            throw;
        }
        finally
        {
            _listener?.Dispose();
        }
    }

    private async Task ProcessClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var clientCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        clientCts.CancelAfter(TimeSpan.FromSeconds(10));

        await using var session = new ClientSession(client);
        using var reader = new StreamReader(session.Stream, Encoding.UTF8, leaveOpen: true);

        using var _ = client;
        client.NoDelay = true;

        try
        {
            await session.SendAsync("WELCOME: Service v1.0. Type 'LOGIN admin secret123'", clientCts.Token);

            string? line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string cmdName = parts[0];

                string[] args = parts.Length > 1 ? parts[1..] : [];

                if (commandFactory.GetCommand(cmdName) is not null and ICommand command)
                {
                    await command.ExecuteAsync(session, args, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }
        finally
        {
            Console.WriteLine("Клиент задисконекчен!");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _listener?.Stop();

        await ValueTask.CompletedTask;
    }
}
