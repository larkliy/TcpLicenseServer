using Microsoft.EntityFrameworkCore;
using Serilog;
using TcpLicenseServer.Data;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands.Admin.User;

public class UserSubscriptionDateUpdateCommand : ICommand
{
    public async ValueTask ExecuteAsync(SessionRegistry sessionRegistry, ClientSession session, string[] args, CancellationToken ct)
    {
        if (args.Length < 2)
        {
            await session.SendAsync("ERROR: Too few arguments.", ct);
            return;
        }

        string userKey = args[0];
        bool isSuccessPass = DateTime.TryParse(args[1], out var newSubscriptionDatetime);

        try
        {
            await using var dbContext = new AppDbContext();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Key == userKey, ct).ConfigureAwait(false);

            if (user == null)
            {
                await session.SendAsync("ERROR: User does not exist.", ct);
                return;
            }

            user.SubscriptionEndDate = isSuccessPass ? newSubscriptionDatetime : user.SubscriptionEndDate.AddDays(1);

            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error changing user SubscriptionDate.");
            await session.SendAsync("ERROR: Internal server error during changing user SubscriptionDate.", ct);
        }
    }
}
