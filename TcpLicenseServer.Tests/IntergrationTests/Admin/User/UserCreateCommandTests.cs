using Microsoft.EntityFrameworkCore;
using TcpLicenseServer.Commands.Admin.User;
using TcpLicenseServer.Data;

namespace TcpLicenseServer.Tests.IntergrationTests.Admin.User;

public class UserCreateCommandTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldCreateUser()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var command = new UserCreateCommand
        {
            ContextFactory = () => new AppDbContext(options)
        };

        var (session, stream) = SessionHelper.CreateTestSession();
        await command.ExecuteAsync(new SessionRegistry(), session, ["new_key"], CancellationToken.None);

        using var checkDb = new AppDbContext(options);
        Assert.NotNull(checkDb.Users.FirstOrDefault(u => u.Key == "new_key"));
    }

}
