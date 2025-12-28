using TcpLicenseServer.Models;

namespace TcpLicenseServer.Tests.UnitTests.Models;

public class CommandArgsTests
{
    [Fact]
    public void PopString_ShouldReturnNextArg_WhenAvailable()
    {
        // Arrange
        var args = new[] { "arg1", "arg2" };
        var commandArgs = new CommandArgs(args);

        // Act
        var result = commandArgs.PopString();

        // Assert
        Assert.Equal("arg1", result);
    }

    [Fact]
    public void PopInt_ShouldThrow_WhenNotInteger()
    {
        // Arrange
        var args = new[] { "not_int" };
        var commandArgs = new CommandArgs(args);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => commandArgs.PopInt());
    }

    [Fact]
    public void RemainingText_ShouldReturnJoinedRest()
    {
        // Arrange
        var args = new[] { "cmd", "Hello", "World" };
        var commandArgs = new CommandArgs(args);

        commandArgs.PopString();

        // Act
        var result = commandArgs.RemainingText;

        // Assert
        Assert.Equal("Hello World", result);
    }
}