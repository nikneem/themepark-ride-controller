using ThemePark.Shared.Enums;

namespace ThemePark.ControlCenter.Domain;

/// <summary>Lightweight DTO representing a ride as returned from RideService.</summary>
public sealed record Ride(Guid RideId, string Name, RideStatus Status);

/// <summary>Summary of a completed or aborted ride session, persisted to state store.</summary>
public sealed record RideSession(
    Guid SessionId,
    Guid RideId,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string Outcome);

/// <summary>An active chaos event injected into a running workflow.</summary>
public sealed record ChaosEvent(
    Guid EventId,
    Guid RideId,
    ChaosEventType EventType,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? ResolvedAt = null);
