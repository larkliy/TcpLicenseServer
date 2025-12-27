using Serilog;
using TcpLicenseServer.Data;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands;

public class ConfigCreateCommand : ICommand
{
    public async ValueTask ExecuteAsync(SessionRegistry sessionRegistry, ClientSession session, string[] args, CancellationToken ct)
    {
        if (args.Length < 2)
        {
            await session.SendAsync("ERROR: Too few arguments.", ct);
            return;
        }

        string configName = args[0];
        string configValue = args[1];

        try
        {
            await using var dbContext = new AppDbContext();

            var config = new Config
            {
                Name = configName,
                JsonConfig = configValue,
                CreatedAt = DateTime.UtcNow,
                UserId = session.UserId
            };

            await dbContext.Configs.AddAsync(config, ct).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

            await session.SendAsync($"SUCCESS: Config {configName} created.", ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating a config.");
            await session.SendAsync("ERROR: Internal server error during a config creating.", ct);
        }
    }
}
