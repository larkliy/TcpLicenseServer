using System.Net.Sockets;
using System.Text;

namespace TcpLicenseServer.Models;

public class ClientSession(TcpClient client, Stream sslStream)
{
    public TcpClient Client { get; set; } = client;
    public Stream Stream { get; set; } = sslStream;
    public bool IsAuthenticated { get; set; } = false;

    public string? Userkey { get; set; }
    public int UserId { get; set; }
    public string? Role { get; set; }

    public async ValueTask SendAsync(string message, CancellationToken ct)
    {
        ReadOnlyMemory<byte> data = Encoding.UTF8.GetBytes(message + "\n");

        await Stream.WriteAsync(data, ct).ConfigureAwait(false);
    }

    public void Disconnect() => Client.Close();
}
