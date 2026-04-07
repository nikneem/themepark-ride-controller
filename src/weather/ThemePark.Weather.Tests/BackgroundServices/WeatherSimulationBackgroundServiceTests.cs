using Dapr.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ThemePark.EventContracts.Events;
using ThemePark.Shared.Enums;
using ThemePark.Weather.Api.BackgroundServices;
using ThemePark.Weather.Services;
using ThemePark.Weather.Models;

namespace ThemePark.Weather.Tests.BackgroundServices;

public sealed class WeatherSimulationBackgroundServiceTests
{
    private static WeatherSimulationBackgroundService CreateService(
        IWeatherSimulationEngine engine,
        DaprClient daprClient,
        IConfiguration? config = null)
    {
        config ??= new ConfigurationBuilder().Build();
        return new WeatherSimulationBackgroundService(
            engine,
            daprClient,
            config,
            NullLogger<WeatherSimulationBackgroundService>.Instance);
    }

    [Fact]
    public async Task TickAsync_Calm_does_not_publish_event()
    {
        var engineMock = new Mock<IWeatherSimulationEngine>();
        engineMock.Setup(e => e.GenerateCondition())
            .Returns(new WeatherCondition(WeatherSeverity.Calm, [], DateTimeOffset.UtcNow));

        var daprMock = new Mock<DaprClient>();

        var service = CreateService(engineMock.Object, daprMock.Object);
        await service.TickAsync();

        daprMock.Verify(d => d.PublishEventAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<WeatherAlertEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TickAsync_Mild_publishes_weather_alert_event()
    {
        var condition = new WeatherCondition(WeatherSeverity.Mild, ["Zone-A"], DateTimeOffset.UtcNow);
        var engineMock = new Mock<IWeatherSimulationEngine>();
        engineMock.Setup(e => e.GenerateCondition()).Returns(condition);

        var daprMock = new Mock<DaprClient>();
        daprMock.Setup(d => d.PublishEventAsync(
            "themepark-pubsub", "weather.alert",
            It.Is<WeatherAlertEvent>(e => e.Severity == WeatherSeverity.Mild && e.AffectedZones.Contains("Zone-A")),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(engineMock.Object, daprMock.Object);
        await service.TickAsync();

        daprMock.Verify(d => d.PublishEventAsync(
            "themepark-pubsub", "weather.alert",
            It.Is<WeatherAlertEvent>(e => e.Severity == WeatherSeverity.Mild),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TickAsync_Severe_publishes_weather_alert_event()
    {
        var condition = new WeatherCondition(WeatherSeverity.Severe, ["Zone-B", "Zone-C"], DateTimeOffset.UtcNow);
        var engineMock = new Mock<IWeatherSimulationEngine>();
        engineMock.Setup(e => e.GenerateCondition()).Returns(condition);

        var daprMock = new Mock<DaprClient>();
        daprMock.Setup(d => d.PublishEventAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<WeatherAlertEvent>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(engineMock.Object, daprMock.Object);
        await service.TickAsync();

        daprMock.Verify(d => d.PublishEventAsync(
            "themepark-pubsub", "weather.alert",
            It.Is<WeatherAlertEvent>(e =>
                e.Severity == WeatherSeverity.Severe &&
                e.AffectedZones.Length == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TickAsync_publishes_event_with_all_required_fields()
    {
        var condition = new WeatherCondition(WeatherSeverity.Severe, ["Zone-A"], DateTimeOffset.UtcNow);
        var engineMock = new Mock<IWeatherSimulationEngine>();
        engineMock.Setup(e => e.GenerateCondition()).Returns(condition);

        WeatherAlertEvent? captured = null;
        var daprMock = new Mock<DaprClient>();
        daprMock.Setup(d => d.PublishEventAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<WeatherAlertEvent>(),
            It.IsAny<CancellationToken>()))
            .Callback<string, string, WeatherAlertEvent, CancellationToken>((_, _, evt, _) => captured = evt)
            .Returns(Task.CompletedTask);

        var service = CreateService(engineMock.Object, daprMock.Object);
        await service.TickAsync();

        Assert.NotNull(captured);
        Assert.NotEqual(Guid.Empty, captured!.EventId);
        Assert.NotEmpty(captured.AffectedZones);
        Assert.True(captured.GeneratedAt > DateTimeOffset.MinValue);
    }
}
