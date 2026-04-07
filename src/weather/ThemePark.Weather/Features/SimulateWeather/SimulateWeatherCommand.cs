namespace ThemePark.Weather.Features.SimulateWeather;

public sealed record SimulateWeatherCommand(string Severity, string[] AffectedZones);
