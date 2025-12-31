using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using TcpLicenseServer.Commands.Config;
using TcpLicenseServer.Data;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Tests.IntergrationTests.Config;

public class ConfigCreateCommandTests
{
    private DbContextOptions<AppDbContext> CreateNewDbOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateConfig()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var user = new User { Id = 1, Key = "test_user", Role = "User" };
        await using (var context = new AppDbContext(options))
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var command = new ConfigCreateCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        session.IsAuthenticated = true;
        session.UserId = user.Id;
        var args = new[] { "new_config", "{\"key\":\"value\"}" };

        // Act
        await command.ExecuteAsync(new SessionRegistry(), session, args, CancellationToken.None);

        // Assert
        await using (var context = new AppDbContext(options))
        {
            var config = await context.Configs.FirstOrDefaultAsync(c => c.Name == "new_config");
            Assert.NotNull(config);
            Assert.Equal("{\"key\":\"value\"}", config.JsonConfig);
            Assert.Equal(user.Id, config.UserId);
        }
        var response = SessionHelper.GetResponse(stream);
        Assert.Contains("SUCCESS: Config new_config created.", response);
    }
}
