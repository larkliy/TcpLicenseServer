
using TcpLicenseServer;

var commandFactory = new CommandFactory();

var server = new MainServer(commandFactory, 8080);

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();

    Console.WriteLine("Сервер остановлен!");
};

try
{
    await server.StartAsync(cts.Token);
}
finally
{
    await server.DisposeAsync();
}