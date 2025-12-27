using Microsoft.EntityFrameworkCore;
using Serilog;
using TcpLicenseServer.Extensions;
using TcpLicenseServer.Attributes;
using TcpLicenseServer.Data;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands.Admin.User;

[AdminOnly]
public class GetAllUsersCommand : ICommand
{
    public async ValueTask ExecuteAsync(SessionRegistry sessionRegistry, ClientSession session, string[] args, CancellationToken ct)
    {
        var commandArgs = new CommandArgs(args);

        try
        {
            int pageNumber = commandArgs.HasNext() ? commandArgs.PopInt() : 1;
            int pageSize = commandArgs.HasNext() ? commandArgs.PopInt() : 10;

            if (pageNumber < 1) pageNumber = 1;

            await using var dbContext = new AppDbContext();

            var users = await dbContext.Users
                .AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .OrderByDescending(c => c.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            await session.ReplyJsonAsync(users, ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting information about all users.");
            await session.ReplyErrorAsync("Internal error retrieving information about all users.", ct);
        }
    }
}
