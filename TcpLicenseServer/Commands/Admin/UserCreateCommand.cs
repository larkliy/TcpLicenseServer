using TcpLicenseServer.Attributes;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands.Admin;

[AdminOnly]
public class UserCreateCommand : ICommand
{
    public ValueTask ExecuteAsync(SessionRegistry sessionRegistry, ClientSession session, string[] args, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
