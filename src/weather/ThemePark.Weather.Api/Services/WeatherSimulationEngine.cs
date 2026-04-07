using Microsoft.Extensions.Options;
using ThemePark.Shared.Enums;
using ThemePark.Weather.Api.Configuration;
using ThemePark.Weather.Models;

namespace ThemePark.Weather.Api.Services;

public sealed class WeatherSimulationEngine : IWeatherSimulationEngine
{
    private readonly WeatherOptions _options;
    private volatile WeatherCondition _current;

    public WeatherSimulationEngine(IOptions<WeatherOptions> options)
    {
        _options = options.Value;
        _current = WeatherCondition.Calm();
    }

    public WeatherCondition CurrentCondition => _current;

    public WeatherCondition GenerateCondition()
    {
        var severity = PickSeverity();
        var zones = severity == WeatherSeverity.Calm ? [] : PickZones();
        var condition = new WeatherCondition(severity, zones, DateTimeOffset.UtcNow);
        _current = condition;
        return condition;
    }

    public void ForceCondition(WeatherCondition condition)
    {
        _current = condition;
    }

    private WeatherSeverity PickSeverity()
    {
        var totalWeight = _options.CalmWeight + _options.MildWeight + _options.SevereWeight;
        var roll = Random.Shared.Next(totalWeight);

        if (roll < _options.CalmWeight) return WeatherSeverity.Calm;
        if (roll < _options.CalmWeight + _options.MildWeight) return WeatherSeverity.Mild;
        return WeatherSeverity.Severe;
    }

    private string[] PickZones()
    {
        var zones = _options.Zones;
        if (zones.Length == 0) return [];

        // Pick at least one, up to all zones
        var count = Random.Shared.Next(1, zones.Length + 1);
        return zones.OrderBy(_ => Random.Shared.Next()).Take(count).ToArray();
    }
}
