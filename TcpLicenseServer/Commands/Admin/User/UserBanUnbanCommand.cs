using Microsoft.EntityFrameworkCore;
using Serilog;
using TcpLicenseServer.Attributes;
using TcpLicenseServer.Data;
using TcpLicenseServer.Extensions;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands.Admin.User;

[AdminOnly]
public class UserBanUnbanCommand : ICommand
{
    public async ValueTask ExecuteAsync(SessionRegistry sessionRegistry, ClientSession session, string[] args, CancellationToken ct)
    {
        var commandArgs = new CommandArgs(args);

        try
        {
            commandArgs.EnsureCount(1);
            string userKey = commandArgs.PopString();

            await using var dbContext = new AppDbContext();

            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Key == userKey, ct)
                .ConfigureAwait(false);

            if (user == null)
            {
                await session.ReplyErrorAsync("User does not exist.", ct);
                return;
            }

            user.IsBanned = !user.IsBanned;

            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            await session.ReplySuccessAsync("Successfully.", ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error handling user ban.");
            await session.ReplyErrorAsync("Internal error while managing user ban.", ct);
        }
    }
}
