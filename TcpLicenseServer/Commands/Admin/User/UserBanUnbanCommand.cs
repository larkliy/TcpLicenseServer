using Microsoft.EntityFrameworkCore;
using Serilog;
using TcpLicenseServer.Attributes;
using TcpLicenseServer.Data;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands.Admin.User;

[AdminOnly]
public class UserBanUnbanCommand : ICommand
{
    public async ValueTask ExecuteAsync(SessionRegistry sessionRegistry, ClientSession session, string[] args, CancellationToken ct)
    {
        if (args.Length < 1)
        {
            await session.SendAsync("ERROR: Too few arguments.", ct);
            return;
        }

        string userKey = args[0];

        try
        {
            await using var dbContext = new AppDbContext();

            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Key == userKey, ct)
                .ConfigureAwait(false);

            if (user == null)
            {
                await session.SendAsync("ERROR: User does not exist.", ct);
                return;
            }

            user.IsBanned = !user.IsBanned;

            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error handling user ban.");
            await session.SendAsync("ERROR: Internal error while managing user ban.", ct);
        }
    }
}
