using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using TcpLicenseServer.Commands.Admin.User;
using TcpLicenseServer.Data;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Tests.IntergrationTests.Admin.User;

public class UserSubscriptionDateUpdateCommandTests
{
    private DbContextOptions<AppDbContext> CreateNewDbOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateSubscriptionDate()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var initialDate = DateTime.UtcNow;
        var user = new TcpLicenseServer.Models.User { Key = "test_user", Role = "User", SubscriptionEndDate = initialDate };
        await using (var context = new AppDbContext(options))
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var command = new UserSubscriptionDateUpdateCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        session.IsAuthenticated = true;
        session.Role = "Admin";
        var newDate = initialDate.AddDays(30);
        var args = new[] { "test_user", newDate.ToString("o", CultureInfo.InvariantCulture) };

        // Act
        await command.ExecuteAsync(new SessionRegistry(), session, args, CancellationToken.None);

        // Assert
        await using (var context = new AppDbContext(options))
        {
            var dbUser = await context.Users.FirstAsync();
            Assert.Equal(newDate.ToUniversalTime(), dbUser.SubscriptionEndDate.ToUniversalTime(), TimeSpan.FromSeconds(1));
        }
        var response = SessionHelper.GetResponse(stream);
        Assert.Contains("SUCCESS: Successfully.", response);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnError_WhenUserDoesNotExist()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var command = new UserSubscriptionDateUpdateCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        session.IsAuthenticated = true;
        session.Role = "Admin";
        var args = new[] { "non_existent_user", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) };

        // Act
        await command.ExecuteAsync(new SessionRegistry(), session, args, CancellationToken.None);

        // Assert
        var response = SessionHelper.GetResponse(stream);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnError_WhenInvalidDateFormat()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var user = new TcpLicenseServer.Models.User { Key = "test_user", Role = "User", SubscriptionEndDate = DateTime.UtcNow };
        await using (var context = new AppDbContext(options))
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var command = new UserSubscriptionDateUpdateCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        session.IsAuthenticated = true;
        session.Role = "Admin";
        var args = new[] { "test_user", "not_a_date" };

        // Act
        await command.ExecuteAsync(new SessionRegistry(), session, args, CancellationToken.None);

        // Assert
        var response = SessionHelper.GetResponse(stream);
        Assert.Contains("ERROR: Internal server error during changing user SubscriptionDate.", response);
    }
}