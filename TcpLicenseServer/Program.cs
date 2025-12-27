
using TcpLicenseServer;
using TcpLicenseServer.Data;

var commandFactory = new CommandFactory();

var server = new MainServer(commandFactory, 8080);

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();

    Console.WriteLine("The server has stopped!");
};

try
{
    await using var dbContext = new AppDbContext();
    await dbContext.Database.EnsureCreatedAsync();

    await server.StartAsync(cts.Token);
}
finally
{
    await server.DisposeAsync();
}