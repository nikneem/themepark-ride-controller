using System.Net;
using System.Text;
using System.Text.Json;
using ThemePark.EventContracts.Events;
using ThemePark.EventContracts.Serialization;

namespace ThemePark.ControlCenter.Tests.PubSub;

/// <summary>
/// Integration tests for the maintenance.completed Dapr subscriber endpoint.
/// Verifies the Control Center API subscriber is correctly registered and accepts MaintenanceCompletedEvent payloads.
/// Note: workflow-step signalling is deferred to the ride-lifecycle-state-machine change.
/// </summary>
public sealed class MaintenanceCompletedSubscriberTests : IClassFixture<ControlCenterApiFactory>
{
    private readonly HttpClient _client;

    public MaintenanceCompletedSubscriberTests(ControlCenterApiFactory factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task MaintenanceCompletedSubscriber_WithValidPayload_Returns200Ok()
    {
        var evt = new MaintenanceCompletedEvent(
            EventId: Guid.NewGuid(),
            MaintenanceId: Guid.NewGuid().ToString(),
            RideId: Guid.NewGuid(),
            CompletedAt: DateTimeOffset.UtcNow);

        var response = await PostEventAsync("/events/maintenance-completed", evt);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MaintenanceCompletedSubscriber_WithMultipleEvents_AllReturn200Ok()
    {
        var rideId = Guid.NewGuid();
        var events = Enumerable.Range(0, 3).Select(_ => new MaintenanceCompletedEvent(
            EventId: Guid.NewGuid(),
            MaintenanceId: Guid.NewGuid().ToString(),
            RideId: rideId,
            CompletedAt: DateTimeOffset.UtcNow));

        foreach (var evt in events)
        {
            var response = await PostEventAsync("/events/maintenance-completed", evt);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private Task<HttpResponseMessage> PostEventAsync<T>(string url, T evt)
    {
        var json = JsonSerializer.Serialize(evt, EventContractsJsonOptions.Default);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return _client.PostAsync(url, content);
    }
}
