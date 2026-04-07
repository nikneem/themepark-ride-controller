using ThemePark.Shared.Enums;

namespace ThemePark.Weather.Models;

public sealed record WeatherCondition(
    WeatherSeverity Severity,
    string[] AffectedZones,
    DateTimeOffset GeneratedAt)
{
    public static WeatherCondition Calm() =>
        new(WeatherSeverity.Calm, [], DateTimeOffset.UtcNow);
}
