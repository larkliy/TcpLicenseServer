using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using TcpLicenseServer.Commands.Admin.User;
using TcpLicenseServer.Data;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Tests.IntergrationTests.Admin.User;

public class UserHwidUpdateCommandTests
{
    private DbContextOptions<AppDbContext> CreateNewDbOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateHwid()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var user = new TcpLicenseServer.Models.User { Key = "test_user", Role = "User", Hwid = "old_hwid" };
        await using (var context = new AppDbContext(options))
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var command = new UserHwidUpdateCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        session.IsAuthenticated = true;
        session.Role = "Admin";
        var args = new[] { "test_user", "new_hwid" };

        // Act
        await command.ExecuteAsync(new SessionRegistry(), session, args, CancellationToken.None);

        // Assert
        await using (var context = new AppDbContext(options))
        {
            var dbUser = await context.Users.FirstAsync();
            Assert.Equal("new_hwid", dbUser.Hwid);
        }
        var response = SessionHelper.GetResponse(stream);
        Assert.Contains("SUCCESS: Successfully.", response);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnError_WhenUserDoesNotExist()
    {
        // Arrange
        var options = CreateNewDbOptions();
        var command = new UserHwidUpdateCommand { ContextFactory = () => new AppDbContext(options) };
        var (session, stream) = SessionHelper.CreateTestSession();
        session.IsAuthenticated = true;
        session.Role = "Admin";
        var args = new[] { "non_existent_user", "new_hwid" };

        // Act
        await command.ExecuteAsync(new SessionRegistry(), session, args, CancellationToken.None);

        // Assert
        var response = SessionHelper.GetResponse(stream);
        Assert.Contains("ERROR: User does not exist.", response);
    }
}