using ThemePark.Shared.Enums;

namespace ThemePark.EventContracts.Events;

/// <summary>
/// Published by Weather Service on topic "weather.alert" when severity is Mild or Severe.
/// </summary>
public sealed record WeatherAlertEvent(
    Guid EventId,
    WeatherSeverity Severity,
    string[] AffectedZones,
    DateTimeOffset GeneratedAt);
