using ThemePark.Shared.Enums;

namespace ThemePark.Shared.Records;

public sealed record ChaosEvent(
    string EventId,
    ChaosEventType Type,
    string Severity,
    DateTimeOffset ReceivedAt,
    bool Resolved);
