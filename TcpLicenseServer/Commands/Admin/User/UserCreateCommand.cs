using Microsoft.EntityFrameworkCore;
using Serilog;
using TcpLicenseServer.Attributes;
using TcpLicenseServer.Data;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands.Admin.User;

[AdminOnly]
public class UserCreateCommand : ICommand
{
    public async ValueTask ExecuteAsync(SessionRegistry sessionRegistry, ClientSession session, string[] args, CancellationToken ct)
    {
        if (args.Length < 1)
        {
            await session.SendAsync("Usage: USERCREATE <key> [role]", ct);
            return;
        }

        string key = args[0];
        string role = args.Length > 1 ? args[1] : "User";
        DateTime subscriptionEndDate = args.Length > 2 ? DateTime.Parse(args[2]) : DateTime.MaxValue;

        if (string.IsNullOrWhiteSpace(key))
        {
            await session.SendAsync("Error: Key cannot be empty.", ct);
            return;
        }

        try
        {
            await using var db = new AppDbContext();

            bool exists = await db.Users.AnyAsync(u => u.Key == key, ct).ConfigureAwait(false);
            if (exists)
            {
                await session.SendAsync($"Error: User with key '{key}' already exists.", ct);
                return;
            }

            var newUser = new Models.User
            {
                Key = key,
                Role = role,
                IsBanned = false,
                CreatedAt = DateTime.UtcNow,
                SubscriptionEndDate = subscriptionEndDate,
                Hwid = null
            };

            db.Users.Add(newUser);
            await db.SaveChangesAsync(ct);

            Log.Information("New user created via command. Key: {Key}, Role: {Role}, By: {AdminKey}", key, role, session.Userkey);
            await session.SendAsync($"Success: User created. Key: {key}, Role: {role}", ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create user.");
            await session.SendAsync("ERROR: Internal server error while creating user.", ct);
        }
    }
}
