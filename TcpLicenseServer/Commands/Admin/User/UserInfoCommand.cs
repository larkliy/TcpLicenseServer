using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json;
using TcpLicenseServer.Attributes;
using TcpLicenseServer.Data;
using TcpLicenseServer.Extensions;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands.Admin.User;

[AdminOnly]
public class UserInfoCommand : ICommand
{
    public Func<AppDbContext> ContextFactory { get; set; } = () => new AppDbContext();

    public async ValueTask ExecuteAsync(SessionRegistry sessionRegistry, ClientSession session, string[] args, CancellationToken ct)
    {
        var commandArgs = new CommandArgs(args);

        try
        {
            commandArgs.EnsureCount(1);
            string userKey = commandArgs.PopString();

            await using var dbContext = ContextFactory();

            var user = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Key == userKey, ct)
                .ConfigureAwait(false);

            if (user == null)
            {
                await session.SendAsync("ERROR: User does not exist.", ct);
                return;
            }

            await session.ReplyJsonAsync(user, ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving user information.");
            await session.ReplyErrorAsync("Internal error while retrieving user information.", ct);
        }
    }
}
