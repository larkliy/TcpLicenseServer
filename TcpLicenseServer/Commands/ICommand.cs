using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands;

public interface ICommand
{
    ValueTask ExecuteAsync(SessionRegistry sessionRegistry, ClientSession session, string[] args, CancellationToken ct);
}