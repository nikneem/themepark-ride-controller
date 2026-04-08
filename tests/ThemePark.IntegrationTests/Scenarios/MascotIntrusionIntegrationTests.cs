using ThemePark.IntegrationTests.Harness;
using ThemePark.Shared.Catalog;
using ThemePark.Shared.Enums;

namespace ThemePark.IntegrationTests.Scenarios;

[Trait("Category", "Integration")]
public sealed class MascotIntrusionIntegrationTests : IClassFixture<AppHostFixture>
{
    private readonly AppHostFixture _fixture;

    public MascotIntrusionIntegrationTests(AppHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StartRide_MascotIntrusionInjected_PausesAndResumesToCompleted()
    {
        var httpClient = _fixture.CreateHttpClient("gateway");
        var harness = new RideWorkflowTestHarness(httpClient);
        var injector = new ChaosEventInjector(httpClient);
        var rideId = RideCatalog.SplashCanyon.RideId.ToString();

        await harness.StartRideAsync(rideId);
        await harness.WaitForStateAsync(rideId, RideStatus.Running, TimeSpan.FromSeconds(60));

        await injector.MascotIntrusionAsync(rideId);
        await harness.WaitForStateAsync(rideId, RideStatus.Paused, TimeSpan.FromSeconds(30));

        await injector.ClearIntrusionAsync(rideId, "mascot-event-1");
        await harness.WaitForStateAsync(rideId, RideStatus.Running, TimeSpan.FromSeconds(30));
        await harness.WaitForStateAsync(rideId, RideStatus.Completed, TimeSpan.FromSeconds(180));
    }
}
