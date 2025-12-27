using Microsoft.EntityFrameworkCore;
using Serilog;
using TcpLicenseServer.Data;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands;

public class GetConfigCommand : ICommand
{
    public async ValueTask ExecuteAsync(SessionRegistry sessionRegistry,
                                        ClientSession session,
                                        string[] args,
                                        CancellationToken ct)
    {
        if (args.Length < 1)
        {
            await session.SendAsync("ERROR: Too few arguments.", ct);
            return;
        }

        try
        {
            string configName = args[0];

            await using var dbContext = new AppDbContext();

            var config = await dbContext.Configs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == configName && c.UserId == session.UserId, ct)
                .ConfigureAwait(false);

            if (config == null)
            {
                await session.SendAsync("ERROR: Config not found.", ct);
                return;
            }

            await session.SendAsync($"SUCCESS: {config.JsonConfig}", ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting config '{ConfigName}' for user {UserKey}", args[0], session.Userkey);
            await session.SendAsync("ERROR: Internal server error while fetching config.", ct);
        }
    }
}
