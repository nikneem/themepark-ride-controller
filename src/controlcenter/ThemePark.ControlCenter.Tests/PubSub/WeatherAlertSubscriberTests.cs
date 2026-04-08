using System.Net;
using System.Text;
using System.Text.Json;
using ThemePark.EventContracts.Events;
using ThemePark.EventContracts.Serialization;
using ThemePark.Shared.Enums;

namespace ThemePark.ControlCenter.Tests.PubSub;

/// <summary>
/// Integration tests for the weather.alert Dapr subscriber endpoint.
/// Verifies the Control Center API subscriber is correctly registered and accepts WeatherAlertEvent payloads.
/// </summary>
public sealed class WeatherAlertSubscriberTests : IClassFixture<ControlCenterApiFactory>
{
    private readonly HttpClient _client;

    public WeatherAlertSubscriberTests(ControlCenterApiFactory factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task WeatherAlertSubscriber_WithSevereSeverity_Returns200Ok()
    {
        var evt = new WeatherAlertEvent(
            EventId: Guid.NewGuid(),
            Severity: WeatherSeverity.Severe,
            AffectedZones: ["Zone-A", "Zone-B"],
            GeneratedAt: DateTimeOffset.UtcNow);

        var response = await PostEventAsync("/events/weather-alert", evt);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WeatherAlertSubscriber_WithMildSeverity_Returns200Ok()
    {
        var evt = new WeatherAlertEvent(
            EventId: Guid.NewGuid(),
            Severity: WeatherSeverity.Mild,
            AffectedZones: ["Zone-C"],
            GeneratedAt: DateTimeOffset.UtcNow);

        var response = await PostEventAsync("/events/weather-alert", evt);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private Task<HttpResponseMessage> PostEventAsync<T>(string url, T evt)
    {
        var json = JsonSerializer.Serialize(evt, EventContractsJsonOptions.Default);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return _client.PostAsync(url, content);
    }
}
