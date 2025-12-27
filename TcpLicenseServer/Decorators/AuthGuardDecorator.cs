using TcpLicenseServer.Commands;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Decorators;

public class AuthGuardDecorator(ICommand innerCommand) : ICommand
{
    public async ValueTask ExecuteAsync(SessionRegistry sessionRegistry, ClientSession session, string[] args, CancellationToken ct)
    {
        if (!session.IsAuthenticated)
        {
            await session.SendAsync("ERROR 401. Unauthorized.", ct);
            return;
        }

        await innerCommand.ExecuteAsync(sessionRegistry, session, args, ct);
    }
}
