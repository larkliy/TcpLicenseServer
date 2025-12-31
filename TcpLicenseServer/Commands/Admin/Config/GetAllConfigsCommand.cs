using Microsoft.EntityFrameworkCore;
using Serilog;
using TcpLicenseServer.Attributes;
using TcpLicenseServer.Data;
using TcpLicenseServer.Extensions;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands.Admin.Config;

[AdminOnly]
public class GetAllConfigsCommand : ICommand
{
    public Func<AppDbContext> ContextFactory { get; set; } = () => new AppDbContext();

    public async ValueTask ExecuteAsync(SessionRegistry sessionRegistry, ClientSession session, string[] args, CancellationToken ct)
    {
        var commandArgs = new CommandArgs(args);

        try
        {
            int pageNumber = commandArgs.HasNext() ? commandArgs.PopInt() : 1;
            int pageSize = commandArgs.HasNext() ? commandArgs.PopInt() : 10;

            if (pageNumber < 1) pageNumber = 1;

            await using var dbContext = ContextFactory();

            var configs = await dbContext.Configs
                .AsNoTracking()
                .Include(c => c.User)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .OrderByDescending(c => c.Id)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.JsonConfig,
                    c.CreatedAt,
                    OwnerKey = c.User != null ? c.User.Key : "Unknown"
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            await session.ReplyJsonAsync(configs, ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting information about all configs.");
            await session.ReplyErrorAsync("Internal error retrieving information about all configs.", ct);
        }
    }
}
