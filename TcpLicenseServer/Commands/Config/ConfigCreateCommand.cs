using Serilog;
using TcpLicenseServer.Data;
using TcpLicenseServer.Extensions;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands.Config;

public class ConfigCreateCommand : ICommand
{
    public Func<AppDbContext> ContextFactory { get; set; } = () => new AppDbContext();

    public async ValueTask ExecuteAsync(SessionRegistry sessionRegistry, ClientSession session, string[] args, CancellationToken ct)
    {
        var commandArgs = new CommandArgs(args);

        try
        {
            commandArgs.EnsureCount(2);
            string configName = commandArgs.PopString();
            string configValue = commandArgs.RemainingText;

            await using var dbContext = ContextFactory();

            var config = new Models.Config
            {
                Name = configName,
                JsonConfig = configValue,
                CreatedAt = DateTime.UtcNow,
                UserId = session.UserId
            };

            await dbContext.Configs.AddAsync(config, ct).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

            await session.ReplySuccessAsync($"Config {configName} created.", ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating a config.");
            await session.ReplyErrorAsync("Internal server error during a config creating.", ct);
        }
    }
}
