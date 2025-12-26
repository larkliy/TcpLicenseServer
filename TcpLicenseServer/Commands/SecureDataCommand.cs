
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands;

public class SecureDataCommand : ICommand
{
    public async ValueTask ExecuteAsync(ClientSession session, string[] args, CancellationToken ct)
    {

        await session.SendAsync($"DATA: Secret report for {session.Username}...", ct);
    }
}
