using Aspire.Hosting.Testing;
using System.Net.Http.Json;
using ThemePark.IntegrationTests.Harness;
using ThemePark.Shared.Catalog;
using ThemePark.Shared.Enums;

namespace ThemePark.IntegrationTests.E2E;

[Trait("Category", "Integration")]
[Collection("E2E")]
public sealed class DemoValidationTests : IClassFixture<DistributedApplicationFactory<Projects.ThemePark_Aspire_AppHost>>
{
    private readonly DistributedApplicationFactory<Projects.ThemePark_Aspire_AppHost> _factory;

    public DemoValidationTests(DistributedApplicationFactory<Projects.ThemePark_Aspire_AppHost> factory)
    {
        _factory = factory;
    }

    /// <summary>Task 5.1: All 5 rides are Idle at suite start.</summary>
    [Fact]
    public async Task E2E_AllRidesIdle_AtSuiteStart()
    {
        await _factory.StartAsync();
        var httpClient = _factory.CreateHttpClient("gateway");
        var harness = new RideWorkflowTestHarness(httpClient);

        foreach (var ride in RideCatalog.All)
        {
            var state = await harness.GetStateAsync(ride.RideId.ToString());
            Assert.Equal(RideStatus.Idle, state);
        }
    }

    /// <summary>Task 5.2: Full Thunder Mountain demo sequence — all events recorded in history.</summary>
    [Fact]
    public async Task E2E_ThunderMountain_FullDemoSequence_AllEventsRecordedInHistory()
    {
        await _factory.StartAsync();
        var httpClient = _factory.CreateHttpClient("gateway");
        var harness = new RideWorkflowTestHarness(httpClient);
        var injector = new ChaosEventInjector(httpClient);
        var rideId = RideCatalog.ThunderMountain.RideId.ToString();

        // Step 1: Start ride, verify PreFlight → Loading → Running
        await harness.StartRideAsync(rideId);
        await harness.WaitForStateAsync(rideId, RideStatus.PreFlight, TimeSpan.FromSeconds(30));
        await harness.WaitForStateAsync(rideId, RideStatus.Loading, TimeSpan.FromSeconds(30));
        await harness.WaitForStateAsync(rideId, RideStatus.Running, TimeSpan.FromSeconds(30));

        // Step 2: Mild weather → Paused → ClearWeather → Running
        await injector.WeatherMildAsync(rideId);
        await harness.WaitForStateAsync(rideId, RideStatus.Paused, TimeSpan.FromSeconds(30));
        await injector.ClearWeatherAsync(rideId, "weather-event-1");
        await harness.WaitForStateAsync(rideId, RideStatus.Running, TimeSpan.FromSeconds(30));

        // Step 3: Mascot intrusion → Paused → ClearIntrusion → Running
        await injector.MascotIntrusionAsync(rideId);
        await harness.WaitForStateAsync(rideId, RideStatus.Paused, TimeSpan.FromSeconds(30));
        await injector.ClearIntrusionAsync(rideId, "mascot-event-1");
        await harness.WaitForStateAsync(rideId, RideStatus.Running, TimeSpan.FromSeconds(30));

        // Step 4: Mechanical failure → Maintenance → ApproveMaintenance → Running
        await injector.MechanicalFailureAsync(rideId);
        await harness.WaitForStateAsync(rideId, RideStatus.Maintenance, TimeSpan.FromSeconds(30));
        await injector.ApproveMaintenanceAsync(rideId);
        await injector.ClearWeatherAsync(rideId, "maintenance-resolve-1");
        await harness.WaitForStateAsync(rideId, RideStatus.Running, TimeSpan.FromSeconds(60));

        // Step 5: Ride completes (Task 5.3)
        await harness.WaitForStateAsync(rideId, RideStatus.Completed, TimeSpan.FromSeconds(180));

        // Verify history contains expected entries
        var history = await httpClient.GetFromJsonAsync<List<HistoryEntry>>(
            $"/controlcenter/rides/{rideId}/history");
        Assert.NotNull(history);
        Assert.NotEmpty(history);
    }

    private sealed record HistoryEntry(
        Guid SessionId,
        Guid RideId,
        DateTimeOffset StartedAt,
        DateTimeOffset? CompletedAt,
        string Outcome);
}
