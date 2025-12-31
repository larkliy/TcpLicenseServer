using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TcpLicenseServer.Commands.Config;
using TcpLicenseServer.Data;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Tests.IntergrationTests.Config;

public class GetConfigsCommandTests
{
    private DbContextOptions<AppDbContext> CreateNewDbOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnOnlyUserConfigs()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var user1 = new User { Id = 1, Key = "user1", Role = "User" };
        var user2 = new User { Id = 2, Key = "user2", Role = "User" };
        await using (var context = new AppDbContext(options))
        {
            context.Users.AddRange(user1, user2);
            context.Configs.AddRange(
                new TcpLicenseServer.Models.Config { Name = "config1", JsonConfig = "{}", UserId = user1.Id },
                new TcpLicenseServer.Models.Config { Name = "config2", JsonConfig = "{}", UserId = user2.Id },
                new TcpLicenseServer.Models.Config { Name = "config3", JsonConfig = "{}", UserId = user1.Id }
            );
            await context.SaveChangesAsync();
        }

        var command = new GetConfigsCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        session.IsAuthenticated = true;
        session.UserId = user1.Id;

        // Act
        await command.ExecuteAsync(new SessionRegistry(), session, new string[] { }, CancellationToken.None);

        // Assert
        stream.Position = 0;
        var reader = new StreamReader(stream);
        var response = await reader.ReadToEndAsync();

        Assert.StartsWith("OK:", response);
        var json = response.Substring("OK:".Length);
        var configs = JsonSerializer.Deserialize<JsonElement[]>(json);
        Assert.NotNull(configs);
        Assert.Equal(2, configs.Length);
    }
}