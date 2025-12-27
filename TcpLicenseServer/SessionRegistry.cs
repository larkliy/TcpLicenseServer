using System.Collections.Concurrent;
using TcpLicenseServer.Models;

namespace TcpLicenseServer;

public class SessionRegistry
{
    private readonly ConcurrentDictionary<string, ClientSession> _sessions = new();

    public bool TryGet(string sessionId, out ClientSession session) 
        => _sessions.TryGetValue(sessionId, out session!);

    public void Register(string sessionId, ClientSession session) 
        => _sessions[sessionId] = session;

    public void Remove(string sessionId)
        => _sessions.Remove(sessionId, out _);
}
