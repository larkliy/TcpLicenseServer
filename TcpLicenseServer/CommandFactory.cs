
using TcpLicenseServer.Commands;
using TcpLicenseServer.Decorators;

namespace TcpLicenseServer;

public class CommandFactory
{
    private readonly Dictionary<string, ICommand> _commands;

    public CommandFactory()
    {
        var loginCmd = new LoginCommand();
        var dataCmd = new SecureDataCommand();

        _commands = new(StringComparer.OrdinalIgnoreCase)
        {
            ["LOGIN"] = loginCmd,
            ["DATA"] = new AuthGuardDecorator(dataCmd)
        };
    }

    public ICommand? GetCommand(string commandName)
    {
        return _commands.TryGetValue(commandName, out var command) ? command : null;
    }
}
