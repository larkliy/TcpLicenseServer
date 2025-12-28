using Serilog;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using TcpLicenseServer.Commands;
using TcpLicenseServer.Models;

namespace TcpLicenseServer;

public class MainServer : IAsyncDisposable
{
    private TcpListener? _listener;
    private readonly CommandFactory _commandFactory;
    private readonly SessionRegistry _sessionRegistry;
    private readonly X509Certificate2 _serverCertificate;
    private readonly SslServerAuthenticationOptions _sslOptions;
    private readonly int _port;


    public MainServer(SessionRegistry sessionRegistry,
                      CommandFactory commandFactory,
                      int port)
    {
        _sessionRegistry = sessionRegistry;
        _commandFactory = commandFactory;
        _port = port;

        _sslOptions = new SslServerAuthenticationOptions
        {
            ServerCertificate = _serverCertificate,
            ClientCertificateRequired = false,
            CertificateRevocationCheckMode = X509RevocationMode.Online,
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
        };

        _serverCertificate = X509CertificateLoader.LoadPkcs12FromFile("C:\\server.pfx", "password123");
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
        ClientSession? session = null;
        SslStream? sslStream = null;

        try
        {
            client.NoDelay = true;

            var networkStream = client.GetStream();
            sslStream = new SslStream(networkStream, leaveInnerStreamOpen: false);

            var handshakeCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, handshakeCts.Token);

            await sslStream.AuthenticateAsServerAsync(_sslOptions, linkedCts.Token).ConfigureAwait(false);

            session = new ClientSession(client, sslStream);

            using var reader = new StreamReader(sslStream, Encoding.UTF8, leaveOpen: true);

            await session.SendAsync("WELCOME: Service v1.0 (Secure).", ct);

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
        catch (AuthenticationException authEx)
        {
            Log.Warning("SSL Handshake failed: {Message}", authEx.Message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in client processing");
        }
        finally
        {
            if (session?.Userkey is not null)
                _sessionRegistry.Remove(session.Userkey);

            if (sslStream is not null)
                await sslStream.DisposeAsync();
            else
                client.Dispose();

            Log.Information("The client has been disconnected.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _listener?.Stop();
        _serverCertificate?.Dispose();

        await ValueTask.CompletedTask;
    }
}
