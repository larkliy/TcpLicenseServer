using Microsoft.EntityFrameworkCore;
using Serilog;
using TcpLicenseServer.Attributes;
using TcpLicenseServer.Data;
using TcpLicenseServer.Extensions;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Commands.Admin.User;

[AdminOnly]
public class UserCreateCommand : ICommand
{
    public async ValueTask ExecuteAsync(SessionRegistry sessionRegistry, ClientSession session, string[] rawArgs, CancellationToken ct)
    {
        try
        {
            var args = new CommandArgs(rawArgs);

            string key = args.PopString();
            string role = args.HasNext() ? args.PopString() : "User";
            DateTime subEnd = args.HasNext() ? args.PopDate() : DateTime.MaxValue;

            await using var db = new AppDbContext();

            bool exists = await db.Users.AnyAsync(u => u.Key == key, ct).ConfigureAwait(false);
            if (exists)
            {
                await session.ReplyErrorAsync($"User with key '{key}' already exists.", ct);
                return;
            }

            var newUser = new Models.User
            {
                Key = key,
                Role = role,
                IsBanned = false,
                CreatedAt = DateTime.UtcNow,
                SubscriptionEndDate = subEnd,
                Hwid = null
            };

            db.Users.Add(newUser);
            await db.SaveChangesAsync(ct);

            Log.Information("New user created via command. Key: {Key}, Role: {Role}, By: {AdminKey}", key, role, session.Userkey);

            await session.ReplySuccessAsync($"User created. Key: {key}, Role: {role}", ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create user.");
            await session.ReplyErrorAsync("Internal server error while creating user.", ct);
        }
    }
}
