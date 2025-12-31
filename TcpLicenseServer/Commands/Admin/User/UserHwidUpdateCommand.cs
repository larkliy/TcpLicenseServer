using Microsoft.EntityFrameworkCore;
using Serilog;
using TcpLicenseServer.Attributes;
using TcpLicenseServer.Data;
using TcpLicenseServer.Extensions;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands.Admin.User;

[AdminOnly]
public class UserHwidUpdateCommand : ICommand
{
    public Func<AppDbContext> ContextFactory { get; set; } = () => new AppDbContext();

    public async ValueTask ExecuteAsync(SessionRegistry sessionRegistry, ClientSession session, string[] args, CancellationToken ct)
    {
        var commandArgs = new CommandArgs(args);

        try
        {
            commandArgs.EnsureCount(2);
            string userKey = commandArgs.PopString();
            string newHwid = commandArgs.PopString();

            await using var dbContext = ContextFactory();

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Key == userKey, ct).ConfigureAwait(false);

            if (user == null)
            {
                await session.ReplyErrorAsync("User does not exist.", ct);
                return;
            }

            user.Hwid = newHwid;

            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            await session.ReplySuccessAsync("Successfully.", ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error changing user Hwid.");
            await session.ReplyErrorAsync("Internal server error during hwid changing.", ct);
        }
    }
}
