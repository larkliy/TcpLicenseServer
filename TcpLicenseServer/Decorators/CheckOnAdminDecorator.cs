using TcpLicenseServer.Commands;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Decorators;

public class CheckOnAdminDecorator(ICommand innerCommand) : ICommand
{
    public async ValueTask ExecuteAsync(SessionRegistry sessionRegistry, ClientSession session, string[] args, CancellationToken ct)
    {
        if (session.Role != "Admin")
        {
            await session.SendAsync("ERROR: You do not have permission to execute this command.", ct);
            return;
        }

        await innerCommand.ExecuteAsync(sessionRegistry, session, args, ct);
    }
}
