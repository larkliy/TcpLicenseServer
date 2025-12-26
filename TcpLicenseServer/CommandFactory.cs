using System.Reflection;
using TcpLicenseServer.Attributes;
using TcpLicenseServer.Commands;
using TcpLicenseServer.Decorators;

namespace TcpLicenseServer;

public class CommandFactory
{
    private readonly Dictionary<string, ICommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public CommandFactory()
    {
        var commandTypes = Assembly.GetExecutingAssembly()
            .GetTypes().Where(t => typeof(ICommand).IsAssignableFrom(t)
            && !t.IsInterface && !t.IsAbstract && !t.Name.EndsWith("Decorator"));

        foreach (var type in commandTypes)
        {
            var instance = (ICommand)Activator.CreateInstance(type)!;

            string cmdName = type.Name.Replace("Command", "").ToUpper();

            ICommand decorated = instance;

            if (type.GetCustomAttribute<AdminOnlyAttribute>() != null)
            {
                decorated = new CheckOnAdminDecorator(decorated);
            }

            if (type != typeof(LoginCommand))
            {
                decorated = new AuthGuardDecorator(decorated);
            }

            _commands[cmdName] = decorated;
        }
    }

    public ICommand? GetCommand(string commandName)
    {
        return _commands.TryGetValue(commandName, out var command) ? command : null;
    }
}
