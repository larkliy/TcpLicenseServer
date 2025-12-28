using System.Net.Sockets;
using System.Text;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Tests;

public static class SessionHelper
{
    public static (ClientSession session, MemoryStream stream) CreateTestSession()
    {
        var memoryStream = new MemoryStream();

        var dummyClient = new TcpClient();

        var session = new ClientSession(dummyClient, memoryStream);

        return (session, memoryStream);
    }

    public static string GetResponse(MemoryStream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return reader.ReadToEnd();
    }
}
