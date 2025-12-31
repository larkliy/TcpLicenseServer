using Microsoft.EntityFrameworkCore;
using Serilog;
using TcpLicenseServer.Data;
using TcpLicenseServer.Extensions;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands;

public class LoginCommand : ICommand
{
    public Func<AppDbContext> ContextFactory { get; set; } = () => new AppDbContext();

    public async ValueTask ExecuteAsync(SessionRegistry sessionRegistry,
                                        ClientSession session,
                                        string[] args,
                                        CancellationToken ct)
    {
        var commandArgs = new CommandArgs(args);
        if (!await ValidateLoginArgsAsync(session, commandArgs, ct)) return;

        try
        {
            commandArgs.EnsureCount(2);

            string key = commandArgs.PopString();
            string hwid = commandArgs.PopString();

            await using var dbContext = ContextFactory();

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Key == key, ct).ConfigureAwait(false);

            if (user == null)
            {
                await session.ReplyErrorAsync("User does not exist.", ct);
                return;
            }

            if (user.IsBanned)
            {
                await session.ReplyErrorAsync("User blocked.", ct);
                return;
            }

            if (hwid != user.Hwid && user.Hwid is not null)
            {
                await session.ReplyErrorAsync("Hwid does not match.", ct);
                return;
            }
            
            if (user.SubscriptionEndDate <= DateTime.UtcNow)
            {
                 await session.ReplyErrorAsync($"Subscription expired. Expiration date: {user.SubscriptionEndDate}", ct);
                 return;
            }

            user.Hwid ??= hwid;
            await AuthenticateSessionAsync(sessionRegistry, session, user, ct);
            
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Login failed for key: {Key}", args.Length > 0 ? args[0] : "unknown");
            await session.ReplyErrorAsync("Internal server error during login.", ct);
        }
    }

    private static async Task<bool> ValidateLoginArgsAsync(ClientSession session, CommandArgs args, CancellationToken ct)
    {
        if (session.IsAuthenticated)
        {
            await session.ReplyInfoAsync("Already logged in.", ct);
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
        clientSession.Role = user.Role;
        clientSession.Userkey = user.Key;

        if (sessionRegistry.TryGet(clientSession.Userkey, out var existing))
        {
            await existing.ReplyErrorAsync("You were disconnected. This key was used elsewhere.", ct);
            existing.Disconnect();
        }

        sessionRegistry.Register(clientSession.Userkey, clientSession);

        await clientSession.ReplySuccessAsync("You have successfully logged in.", ct);
    }
}
