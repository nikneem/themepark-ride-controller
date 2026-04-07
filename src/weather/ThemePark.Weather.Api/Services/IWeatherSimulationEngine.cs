using ThemePark.Weather.Models;

namespace ThemePark.Weather.Api.Services;

public interface IWeatherSimulationEngine
{
    WeatherCondition CurrentCondition { get; }
    WeatherCondition GenerateCondition();
    void ForceCondition(WeatherCondition condition);
}
