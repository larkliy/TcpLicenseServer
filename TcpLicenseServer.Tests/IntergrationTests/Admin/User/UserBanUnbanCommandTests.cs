using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using TcpLicenseServer.Commands.Admin.User;
using TcpLicenseServer.Data;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Tests.IntergrationTests.Admin.User;

public class UserBanUnbanCommandTests
{
    private DbContextOptions<AppDbContext> CreateNewDbOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task ExecuteAsync_ShouldBanUser_WhenUserIsNotBanned()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var user = new TcpLicenseServer.Models.User { Key = "test_user", Role = "User", IsBanned = false };
        await using (var context = new AppDbContext(options))
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var command = new UserBanUnbanCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        session.IsAuthenticated = true;
        session.Role = "Admin";
        var args = new[] { "test_user" };

        // Act
        await command.ExecuteAsync(new SessionRegistry(), session, args, CancellationToken.None);

        // Assert
        await using (var context = new AppDbContext(options))
        {
            var dbUser = await context.Users.FirstAsync(u => u.Key == "test_user");
            Assert.True(dbUser.IsBanned);
        }
        var response = SessionHelper.GetResponse(stream);
        Assert.Contains("SUCCESS: Successfully.", response);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUnbanUser_WhenUserIsBanned()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var user = new TcpLicenseServer.Models.User { Key = "test_user", Role = "User", IsBanned = true };
        await using (var context = new AppDbContext(options))
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var command = new UserBanUnbanCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        session.IsAuthenticated = true;
        session.Role = "Admin";
        var args = new[] { "test_user" };

        // Act
        await command.ExecuteAsync(new SessionRegistry(), session, args, CancellationToken.None);

        // Assert
        await using (var context = new AppDbContext(options))
        {
            var dbUser = await context.Users.FirstAsync(u => u.Key == "test_user");
            Assert.False(dbUser.IsBanned);
        }
        var response = SessionHelper.GetResponse(stream);
        Assert.Contains("SUCCESS: Successfully.", response);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnError_WhenUserDoesNotExist()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var command = new UserBanUnbanCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        session.IsAuthenticated = true;
        session.Role = "Admin";
        var args = new[] { "non_existent_user" };

        // Act
        await command.ExecuteAsync(new SessionRegistry(), session, args, CancellationToken.None);

        // Assert
        var response = SessionHelper.GetResponse(stream);
        Assert.Contains("ERROR: User does not exist.", response);
    }
}