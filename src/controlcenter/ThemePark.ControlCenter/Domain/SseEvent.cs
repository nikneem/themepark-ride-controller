namespace ThemePark.ControlCenter.Domain;

/// <summary>
/// Payload sent over the SSE stream to connected frontend clients.
/// <see cref="Data"/> is a pre-serialised JSON string ready to emit as the <c>data:</c> line.
/// </summary>
public sealed record SseEvent(string EventType, string Data, DateTimeOffset OccurredAt);
