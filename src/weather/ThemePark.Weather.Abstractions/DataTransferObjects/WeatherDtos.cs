namespace ThemePark.Weather.Abstractions.DataTransferObjects;

public sealed record SimulateWeatherRequest(string Severity, IReadOnlyList<string> AffectedZones);

public sealed record CurrentWeatherResponse(string Severity, IReadOnlyList<string> AffectedZones, DateTimeOffset GeneratedAt);
