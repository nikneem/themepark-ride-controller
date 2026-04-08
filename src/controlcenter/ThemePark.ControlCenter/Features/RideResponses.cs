namespace ThemePark.ControlCenter.Features;

/// <summary>Lightweight ride summary returned by the list-all-rides endpoint.</summary>
public sealed record RideDto(Guid RideId, string Name, string Status);

/// <summary>Detailed ride status including active workflow step and chaos events.</summary>
public sealed record RideStatusResponse(
    Guid RideId,
    string Name,
    string Status,
    string? WorkflowStep,
    IReadOnlyList<string> ActiveChaosEvents);

/// <summary>Response returned when a ride workflow session is successfully started.</summary>
public sealed record StartRideResponse(string WorkflowInstanceId);

/// <summary>A single historical ride session summary returned by the ride-history endpoint.</summary>
public sealed record RideHistoryEntry(
    Guid SessionId,
    Guid RideId,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string Outcome);
