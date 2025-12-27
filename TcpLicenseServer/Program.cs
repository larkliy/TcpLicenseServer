
using Serilog;
using TcpLicenseServer;
using TcpLicenseServer.Data;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var commandFactory = new CommandFactory();
var sessionRegistry = new SessionRegistry();

var server = new MainServer(sessionRegistry, commandFactory, 8080);

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();

    Log.Information("You stopped the server by pressing CTRL + C.");
};

try
{
    await using var dbContext = new AppDbContext();
    await dbContext.Database.EnsureCreatedAsync();

    Log.Information("The server is running!");

    await server.StartAsync(cts.Token);

}
finally
{
    await server.DisposeAsync();
    Log.CloseAndFlush();
}