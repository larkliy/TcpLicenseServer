using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TcpLicenseServer.Commands.Admin.Config;
using TcpLicenseServer.Data;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Tests.IntergrationTests.Admin.Config;

public class GetAllConfigsCommandTests
{
    private DbContextOptions<AppDbContext> CreateNewDbOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnAllConfigs()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var user = new TcpLicenseServer.Models.User { Key = "test_user", Role = "User" };
        await using (var context = new AppDbContext(options))
        {
            context.Users.Add(user);
            context.Configs.AddRange(
                new TcpLicenseServer.Models.Config { Name = "config1", JsonConfig = "{}", UserId = user.Id },
                new TcpLicenseServer.Models.Config { Name = "config2", JsonConfig = "{}", UserId = user.Id }
            );
            await context.SaveChangesAsync();
        }

        var command = new GetAllConfigsCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        session.IsAuthenticated = true;
        session.Role = "Admin"; 

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