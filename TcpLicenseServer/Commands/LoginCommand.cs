
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands;

public class LoginCommand : ICommand
{
    public async ValueTask ExecuteAsync(ClientSession session, string[] args, CancellationToken ct)
    {
        if (session.IsAuthenticated)
        {
            await session.SendAsync("INFO: Already logged in.", ct);
            return;
        }

        
    }
}
