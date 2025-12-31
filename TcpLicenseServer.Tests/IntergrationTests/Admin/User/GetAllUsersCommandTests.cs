using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TcpLicenseServer.Commands.Admin.User;
using TcpLicenseServer.Data;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Tests.IntergrationTests.Admin.User;

public class GetAllUsersCommandTests
{
    private DbContextOptions<AppDbContext> CreateNewDbOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnAllUsers()
    {
        // Arrange
        var options = CreateNewDbOptions();
        await using (var context = new AppDbContext(options))
        {
            context.Users.AddRange(
                new TcpLicenseServer.Models.User { Key = "user1", Role = "User" },
                new TcpLicenseServer.Models.User { Key = "user2", Role = "User" }
            );
            await context.SaveChangesAsync();
        }

        var command = new GetAllUsersCommand { ContextFactory = () => new AppDbContext(options) };
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
        var users = JsonSerializer.Deserialize<TcpLicenseServer.Models.User[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(users);
        Assert.Equal(2, users.Length);
    }
}