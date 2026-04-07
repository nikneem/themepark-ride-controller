using Dapr.Client;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using ThemePark.EventContracts.Events;
using ThemePark.Shared.Enums;
using ThemePark.Weather.Api.Services;
using ThemePark.Weather.Api.SimulateWeather;
using ThemePark.Weather.Models;

namespace ThemePark.Weather.Tests.SimulateWeather;

public sealed class SimulateWeatherHandlerTests
{
    private static (SimulateWeatherHandler Handler, Mock<IWeatherSimulationEngine> Engine, Mock<DaprClient> Dapr)
        CreateHandler()
    {
        var engineMock = new Mock<IWeatherSimulationEngine>();
        var daprMock = new Mock<DaprClient>();
        daprMock.Setup(d => d.PublishEventAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<WeatherAlertEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (new SimulateWeatherHandler(engineMock.Object, daprMock.Object), engineMock, daprMock);
    }

    [Fact]
    public async Task HandleAsync_Calm_returns_202()
    {
        var (handler, _, _) = CreateHandler();
        var result = await handler.HandleAsync(new SimulateWeatherRequest("Calm", []));
        Assert.IsType<Accepted>(result.Result);
    }

    [Fact]
    public async Task HandleAsync_Mild_returns_202()
    {
        var (handler, _, _) = CreateHandler();
        var result = await handler.HandleAsync(new SimulateWeatherRequest("Mild", ["Zone-A"]));
        Assert.IsType<Accepted>(result.Result);
    }

    [Fact]
    public async Task HandleAsync_Severe_returns_202()
    {
        var (handler, _, _) = CreateHandler();
        var result = await handler.HandleAsync(new SimulateWeatherRequest("Severe", ["Zone-B"]));
        Assert.IsType<Accepted>(result.Result);
    }

    [Fact]
    public async Task HandleAsync_invalid_severity_returns_400()
    {
        var (handler, _, _) = CreateHandler();
        var result = await handler.HandleAsync(new SimulateWeatherRequest("Hurricane", []));
        Assert.IsType<BadRequest<string>>(result.Result);
    }

    [Fact]
    public async Task HandleAsync_Calm_does_not_publish_event()
    {
        var (handler, _, dapr) = CreateHandler();
        await handler.HandleAsync(new SimulateWeatherRequest("Calm", []));

        dapr.Verify(d => d.PublishEventAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<WeatherAlertEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_Mild_publishes_weather_alert_event()
    {
        var (handler, _, dapr) = CreateHandler();
        await handler.HandleAsync(new SimulateWeatherRequest("Mild", ["Zone-A", "Zone-B"]));

        dapr.Verify(d => d.PublishEventAsync(
            "themepark-pubsub", "weather.alert",
            It.Is<WeatherAlertEvent>(e => e.Severity == WeatherSeverity.Mild),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Severe_publishes_weather_alert_event()
    {
        var (handler, _, dapr) = CreateHandler();
        await handler.HandleAsync(new SimulateWeatherRequest("Severe", ["Zone-C"]));

        dapr.Verify(d => d.PublishEventAsync(
            "themepark-pubsub", "weather.alert",
            It.Is<WeatherAlertEvent>(e => e.Severity == WeatherSeverity.Severe),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_forces_condition_on_engine()
    {
        var (handler, engine, _) = CreateHandler();
        await handler.HandleAsync(new SimulateWeatherRequest("Severe", ["Zone-A"]));

        engine.Verify(e => e.ForceCondition(
            It.Is<WeatherCondition>(c => c.Severity == WeatherSeverity.Severe)), Times.Once);
    }
}
