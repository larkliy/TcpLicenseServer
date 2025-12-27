using TcpLicenseServer.Models;

namespace TcpLicenseServer.Extensions;

public static class SessionExtensions
{
    extension(ClientSession session)
    {
        public async ValueTask ReplyErrorAsync(string message, CancellationToken ct)
        {
            await session.SendAsync($"ERROR: {message}", ct);
        }

        public async ValueTask ReplySuccessAsync(string message, CancellationToken ct)
        {
            await session.SendAsync($"SUCCESS: {message}", ct);
        }

        public async ValueTask ReplyInfoAsync(string message, CancellationToken ct)
        {
            await session.SendAsync($"INFO: {message}", ct);
        }

        public async ValueTask ReplyJsonAsync<T>(T data, CancellationToken ct)
        {
            await session.SendAsync($"OK: {System.Text.Json.JsonSerializer.Serialize(data)}", ct);
        }
    }
}
