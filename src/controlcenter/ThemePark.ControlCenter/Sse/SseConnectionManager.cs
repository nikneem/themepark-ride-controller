using System.Collections.Concurrent;
using System.Threading.Channels;
using ThemePark.ControlCenter.Domain;

namespace ThemePark.ControlCenter.Sse;

/// <summary>
/// Manages per-connection SSE channels. Each connected SSE client gets its own
/// <see cref="Channel{T}"/> so a slow or disconnected client cannot block others.
/// </summary>
public sealed class SseConnectionManager
{
    private readonly ConcurrentDictionary<string, Channel<SseEvent>> _connections = new();

    /// <summary>Creates a new channel for an incoming SSE client and returns its reader.</summary>
    public (string ConnectionId, ChannelReader<SseEvent> Reader) AddConnection()
    {
        var id = Guid.NewGuid().ToString("N");
        var channel = Channel.CreateUnbounded<SseEvent>(
            new UnboundedChannelOptions { SingleWriter = false, SingleReader = true });
        _connections[id] = channel;
        return (id, channel.Reader);
    }

    /// <summary>Removes and completes the channel for a disconnected client.</summary>
    public void RemoveConnection(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var channel))
            channel.Writer.TryComplete();
    }

    /// <summary>Broadcasts an event to all currently connected SSE clients.</summary>
    public void BroadcastEvent(SseEvent evt)
    {
        foreach (var (_, channel) in _connections)
            channel.Writer.TryWrite(evt);
    }

    /// <summary>Returns the current number of connected SSE clients.</summary>
    public int ConnectionCount => _connections.Count;
}
