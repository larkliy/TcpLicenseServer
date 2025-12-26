
using System.Net.Sockets;
using System.Text;

namespace TcpLicenseServer.Models;

public class ClientSession(TcpClient client)
{
    public TcpClient Client { get; set; } = client;
    public NetworkStream Stream { get; set; } = client.GetStream();
    public bool IsAuthenticated { get; set; } = false;
    public string? Username { get; set; }
    public string? Role { get; set; }

    public async ValueTask SendAsync(string message, CancellationToken cancellationToken)
    {
        ReadOnlyMemory<byte> data = Encoding.UTF8.GetBytes(message + "\n");

        await Stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        return ValueTask.CompletedTask;
    }
}
