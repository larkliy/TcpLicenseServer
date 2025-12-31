using Microsoft.EntityFrameworkCore;
using Serilog;
using TcpLicenseServer.Data;
using TcpLicenseServer.Extensions;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands.Config;

public class GetConfigCommand : ICommand
{
    public Func<AppDbContext> ContextFactory { get; set; } = () => new AppDbContext();

    public async ValueTask ExecuteAsync(SessionRegistry sessionRegistry,
                                        ClientSession session,
                                        string[] args,
                                        CancellationToken ct)
    {
        var commandArgs = new CommandArgs(args);

        try
        {
            commandArgs.EnsureCount(1);
            string configName = commandArgs.PopString();

            await using var dbContext = ContextFactory();

            var config = await dbContext.Configs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == configName && c.UserId == session.UserId, ct)
                .ConfigureAwait(false);

            if (config == null)
            {
                await session.ReplyErrorAsync("Config not found.", ct);
                return;
            }

            await session.ReplySuccessAsync(config.JsonConfig, ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting config '{ConfigName}' for user {UserKey}", args[0], session.Userkey);
            await session.ReplyErrorAsync("Internal server error while fetching config.", ct);
        }
    }
}
