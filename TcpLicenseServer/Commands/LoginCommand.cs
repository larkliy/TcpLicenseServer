using Microsoft.EntityFrameworkCore;
using TcpLicenseServer.Data;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands;

public class LoginCommand : ICommand
{
    public async ValueTask ExecuteAsync(SessionRegistry sessionRegistry,
                                        ClientSession session,
                                        string[] args,
                                        CancellationToken ct)
    {
        if (!await ValidateLoginArgsAsync(session, args, ct)) return;

        await using var dbContext = new AppDbContext();

        string key = args[0];
        string hwid = args[1];

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Key == key, ct).ConfigureAwait(false);

        if (user == null)
        {
            await session.SendAsync("ERROR: User does not exist.", ct);
            return;
        }

        if (hwid != user.Hwid && user.Hwid is not null)
        {
            await session.SendAsync("ERROR: Hwid does not match.", ct);
            return;
        }
        else
        {
            user.Hwid ??= user.Hwid;
            await AuthenticateSessionAsync(sessionRegistry, session, user, ct);
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static async Task<bool> ValidateLoginArgsAsync(ClientSession session, string[] args, CancellationToken ct)
    {
        if (session.IsAuthenticated)
        {
            await session.SendAsync("INFO: Already logged in.", ct);
            return false;
        }

        if (args.Length < 2)
        {
            await session.SendAsync("ERROR: Too few arguments for the command.", ct);
            return false;
        }

        return true;
    }

    private static async Task AuthenticateSessionAsync(SessionRegistry sessionRegistry,
                                                       ClientSession clientSession,
                                                       User user,
                                                       CancellationToken ct)
    {
        clientSession.UserId = user.Id;
        clientSession.IsAuthenticated = true;
        clientSession.Role = "Player";
        clientSession.Userkey = user.Key;

        if (sessionRegistry.TryGet(clientSession.Userkey, out var existing))
        {
            await existing.SendAsync("ERROR: You were disconnected. This key was used elsewhere.", ct);
            existing.Disconnect();
        }

        sessionRegistry.Register(clientSession.Userkey, clientSession);

        await clientSession.SendAsync("SUCCESSFUL: You have successfully logged in.", ct);
    }
}
