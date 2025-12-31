using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TcpLicenseServer.Commands.Admin.User;
using TcpLicenseServer.Data;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Tests.IntergrationTests.Admin.User;

public class UserInfoCommandTests
{
    private DbContextOptions<AppDbContext> CreateNewDbOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnUserInfo()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var user = new TcpLicenseServer.Models.User { Key = "test_user", Role = "TestRole" };
        await using (var context = new AppDbContext(options))
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var command = new UserInfoCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        session.IsAuthenticated = true;
        session.Role = "Admin";
        var args = new[] { "test_user" };

        // Act
        await command.ExecuteAsync(new SessionRegistry(), session, args, CancellationToken.None);

        // Assert
        stream.Position = 0;
        var reader = new StreamReader(stream);
        var response = await reader.ReadToEndAsync();
        Assert.StartsWith("OK:", response);
        var json = response.Substring("OK:".Length);
        var resultUser = JsonSerializer.Deserialize<TcpLicenseServer.Models.User>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(resultUser);
        Assert.Equal("test_user", resultUser.Key);
        Assert.Equal("TestRole", resultUser.Role);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnError_WhenUserDoesNotExist()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var command = new UserInfoCommand { ContextFactory = () => new AppDbContext(options) };
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