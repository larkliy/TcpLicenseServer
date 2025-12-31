using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using TcpLicenseServer.Commands;
using TcpLicenseServer.Data;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Tests.IntergrationTests;

public class LoginCommandTests
{
    private DbContextOptions<AppDbContext> CreateNewDbOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLoginSuccessfully_WhenUserExistsAndHwidIsNull()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var user = new User { Key = "test_key", Role = "User", IsBanned = false, SubscriptionEndDate = DateTime.MaxValue, Hwid = null };
        await using (var context = new AppDbContext(options))
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var command = new LoginCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        var args = new[] { "test_key", "new_hwid" };
        var sessionRegistry = new SessionRegistry();

        // Act
        await command.ExecuteAsync(sessionRegistry, session, args, CancellationToken.None);

        // Assert
        Assert.True(session.IsAuthenticated);
        Assert.Equal(user.Id, session.UserId);
        Assert.Equal("test_key", session.Userkey);

        await using (var context = new AppDbContext(options))
        {
            var dbUser = await context.Users.FirstAsync();
            Assert.Equal("new_hwid", dbUser.Hwid);
        }

        var response = SessionHelper.GetResponse(stream);
        Assert.Contains("SUCCESS: You have successfully logged in.", response);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenUserDoesNotExist()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var command = new LoginCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        var args = new[] { "non_existent_key", "any_hwid" };

        // Act
        await command.ExecuteAsync(new SessionRegistry(), session, args, CancellationToken.None);

        // Assert
        Assert.False(session.IsAuthenticated);
        var response = SessionHelper.GetResponse(stream);
        Assert.Contains("ERROR: User does not exist.", response);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenUserIsBanned()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var user = new User { Key = "banned_key", Role = "User", IsBanned = true, SubscriptionEndDate = DateTime.MaxValue };
        await using (var context = new AppDbContext(options))
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var command = new LoginCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        var args = new[] { "banned_key", "any_hwid" };

        // Act
        await command.ExecuteAsync(new SessionRegistry(), session, args, CancellationToken.None);

        // Assert
        Assert.False(session.IsAuthenticated);
        var response = SessionHelper.GetResponse(stream);
        Assert.Contains("ERROR: User blocked.", response);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenHwidDoesNotMatch()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var user = new User { Key = "hwid_key", Role = "User", IsBanned = false, SubscriptionEndDate = DateTime.MaxValue, Hwid = "correct_hwid" };
        await using (var context = new AppDbContext(options))
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var command = new LoginCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        var args = new[] { "hwid_key", "wrong_hwid" };

        // Act
        await command.ExecuteAsync(new SessionRegistry(), session, args, CancellationToken.None);

        // Assert
        Assert.False(session.IsAuthenticated);
        var response = SessionHelper.GetResponse(stream);
        Assert.Contains("ERROR: Hwid does not match.", response);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenSubscriptionExpired()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var user = new User { Key = "expired_key", Role = "User", IsBanned = false, SubscriptionEndDate = DateTime.UtcNow.AddDays(-1) };
        await using (var context = new AppDbContext(options))
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var command = new LoginCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        var args = new[] { "expired_key", "any_hwid" };

        // Act
        await command.ExecuteAsync(new SessionRegistry(), session, args, CancellationToken.None);

        // Assert
        Assert.False(session.IsAuthenticated);
        var response = SessionHelper.GetResponse(stream);
        Assert.StartsWith("ERROR: Subscription expired.", response);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnInfo_WhenAlreadyLoggedIn()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var command = new LoginCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        session.IsAuthenticated = true;
        var args = new[] { "any_key", "any_hwid" };

        // Act
        await command.ExecuteAsync(new SessionRegistry(), session, args, CancellationToken.None);

        // Assert
        var response = SessionHelper.GetResponse(stream);
        Assert.Contains("INFO: Already logged in.", response);
    }
}