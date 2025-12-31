using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using TcpLicenseServer.Commands.Config;
using TcpLicenseServer.Data;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Tests.IntergrationTests.Config;

public class GetConfigCommandTests
{
    private DbContextOptions<AppDbContext> CreateNewDbOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnConfigJson()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var user = new User { Id = 1, Key = "test_user", Role = "User" };
        var config = new TcpLicenseServer.Models.Config { Name = "my_config", JsonConfig = "{\"data\":\"secret\"}", UserId = user.Id };
        await using (var context = new AppDbContext(options))
        {
            context.Users.Add(user);
            context.Configs.Add(config);
            await context.SaveChangesAsync();
        }

        var command = new GetConfigCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        session.IsAuthenticated = true;
        session.UserId = user.Id;
        var args = new[] { "my_config" };

        // Act
        await command.ExecuteAsync(new SessionRegistry(), session, args, CancellationToken.None);

        // Assert
        var response = SessionHelper.GetResponse(stream);
        Assert.Contains("SUCCESS: {\"data\":\"secret\"}", response);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnError_WhenConfigNotFound()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var user = new User { Id = 1, Key = "test_user", Role = "User" };
        await using (var context = new AppDbContext(options))
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var command = new GetConfigCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        session.IsAuthenticated = true;
        session.UserId = user.Id;
        var args = new[] { "non_existent_config" };

        // Act
        await command.ExecuteAsync(new SessionRegistry(), session, args, CancellationToken.None);

        // Assert
        var response = SessionHelper.GetResponse(stream);
        Assert.Contains("ERROR: Config not found.", response);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnError_WhenConfigBelongsToAnotherUser()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var user1 = new User { Id = 1, Key = "user1", Role = "User" };
        var user2 = new User { Id = 2, Key = "user2", Role = "User" };
        var config = new TcpLicenseServer.Models.Config { Name = "my_config", JsonConfig = "{\"data\":\"secret\"}", UserId = user1.Id };
        await using (var context = new AppDbContext(options))
        {
            context.Users.AddRange(user1, user2);
            context.Configs.Add(config);
            await context.SaveChangesAsync();
        }

        var command = new GetConfigCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        session.IsAuthenticated = true;
        session.UserId = user2.Id; // Authenticated as user2
        var args = new[] { "my_config" };

        // Act
        await command.ExecuteAsync(new SessionRegistry(), session, args, CancellationToken.None);

        // Assert
        var response = SessionHelper.GetResponse(stream);
        Assert.Contains("ERROR: Config not found.", response);
    }
}
