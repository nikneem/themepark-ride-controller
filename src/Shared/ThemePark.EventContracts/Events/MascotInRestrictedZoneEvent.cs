namespace ThemePark.EventContracts.Events;

/// <summary>
/// Published by Mascot Service on topic "mascot.in-restricted-zone" when a mascot enters a ride zone.
/// </summary>
public sealed record MascotInRestrictedZoneEvent(
    Guid EventId,
    string MascotId,
    string MascotName,
    Guid AffectedRideId,
    DateTimeOffset DetectedAt);
