using Dapr.Client;
using ThemePark.Weather.Api.BackgroundServices;
using ThemePark.Weather.Api.Configuration;
using ThemePark.Weather.Api.GetCurrentWeather;
using ThemePark.Weather.Api.Services;
using ThemePark.Weather.Api.SimulateWeather;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDaprClient();
builder.Services.Configure<WeatherOptions>(
    builder.Configuration.GetSection(WeatherOptions.SectionName));
builder.Services.AddSingleton<IWeatherSimulationEngine, WeatherSimulationEngine>();
builder.Services.AddScoped<SimulateWeatherHandler>();
builder.Services.AddHostedService<WeatherSimulationBackgroundService>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseHttpsRedirection();

GetCurrentWeatherEndpoint.Map(app);
SimulateWeatherEndpoint.Map(app);

app.Run();
