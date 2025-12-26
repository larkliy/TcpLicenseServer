using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands;

public interface ICommand
{
    ValueTask ExecuteAsync(ClientSession session, string[] args, CancellationToken ct);
}
