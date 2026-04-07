using Microsoft.Extensions.DependencyInjection;
using ThemePark.Weather.Features.SimulateWeather;

namespace ThemePark.Weather;

public static class WeatherModule
{
    public static IServiceCollection AddWeatherModule(this IServiceCollection services)
    {
        services.AddScoped<SimulateWeatherHandler>();
        return services;
    }
}
