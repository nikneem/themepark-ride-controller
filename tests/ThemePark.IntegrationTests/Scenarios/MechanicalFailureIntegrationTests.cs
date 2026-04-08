using ThemePark.IntegrationTests.Harness;
using ThemePark.Shared.Catalog;
using ThemePark.Shared.Enums;

namespace ThemePark.IntegrationTests.Scenarios;

[Trait("Category", "Integration")]
public sealed class MechanicalFailureIntegrationTests : IClassFixture<AppHostFixture>
{
    private readonly AppHostFixture _fixture;

    public MechanicalFailureIntegrationTests(AppHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StartRide_MechanicalFailureInjected_EntersMaintenanceAndResumesToCompleted()
    {
        var httpClient = _fixture.CreateHttpClient("gateway");
        var harness = new RideWorkflowTestHarness(httpClient);
        var injector = new ChaosEventInjector(httpClient);
        var rideId = RideCatalog.HauntedMansion.RideId.ToString();

        await harness.StartRideAsync(rideId);
        await harness.WaitForStateAsync(rideId, RideStatus.Running, TimeSpan.FromSeconds(60));

        await injector.MechanicalFailureAsync(rideId);
        await harness.WaitForStateAsync(rideId, RideStatus.Maintenance, TimeSpan.FromSeconds(30));

        await injector.ApproveMaintenanceAsync(rideId);
        // Resolve the maintenance chaos event so workflow can resume
        await injector.ClearWeatherAsync(rideId, "maintenance-event-1");
        await harness.WaitForStateAsync(rideId, RideStatus.Running, TimeSpan.FromSeconds(60));
        await harness.WaitForStateAsync(rideId, RideStatus.Completed, TimeSpan.FromSeconds(180));
    }
}
