using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json;
using TcpLicenseServer.Attributes;
using TcpLicenseServer.Data;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands.Admin.User;

[AdminOnly]
public class UserInfoCommand : ICommand
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
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Key == userKey, ct)
                .ConfigureAwait(false);

            if (user == null)
            {
                await session.SendAsync("ERROR: User does not exist.", ct);
                return;
            }

            await session.SendAsync($"OK: {JsonSerializer.Serialize(user)}", ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving user information.");
            await session.SendAsync("ERROR: Internal error while retrieving user information.", ct);
        }
    }
}
