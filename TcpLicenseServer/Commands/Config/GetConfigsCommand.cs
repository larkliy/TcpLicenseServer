using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json;
using TcpLicenseServer.Data;
using TcpLicenseServer.Extensions;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands.Config;

public class GetConfigsCommand : ICommand
{
    public async ValueTask ExecuteAsync(SessionRegistry sessionRegistry, ClientSession session, string[] args, CancellationToken ct)
    {
        try
        {
            await using var dbContext = new AppDbContext();

            var configs = await dbContext.Configs
                .Where(c => c.UserId == session.UserId)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            await session.ReplyJsonAsync(configs, ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error receiving configs.");
            await session.ReplyErrorAsync("Internal error while trying to get list of configs.", ct);
        }
    }
}
