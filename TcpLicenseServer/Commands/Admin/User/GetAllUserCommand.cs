using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json;
using TcpLicenseServer.Data;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands.Admin.User;

public class GetAllUserCommand : ICommand
{
    public async ValueTask ExecuteAsync(SessionRegistry sessionRegistry, ClientSession session, string[] args, CancellationToken ct)
    {
        if (args.Length > 2)
        {
            await session.SendAsync("ERROR: Too few arguments.", ct);
            return;
        }

        int pageNumber = int.Parse(args[0]);
        int pageSize = int.Parse(args[1]);

        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        try
        {
            await using var dbContext = new AppDbContext();

            var users = await dbContext.Users
                .AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .OrderByDescending(c => c.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            await session.SendAsync($"OK: {JsonSerializer.Serialize(users)}", ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting information about all users.");
            await session.SendAsync("ERROR: Internal error retrieving information about all users.", ct);
        }
    }
}
