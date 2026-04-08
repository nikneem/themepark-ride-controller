using ThemePark.IntegrationTests.Harness;
using ThemePark.Shared.Catalog;
using ThemePark.Shared.Enums;
using ThemePark.Tests.Shared.Fakers;

namespace ThemePark.IntegrationTests.Scenarios;

[Trait("Category", "Integration")]
public sealed class HappyPathIntegrationTests : IClassFixture<AppHostFixture>
{
    private readonly AppHostFixture _fixture;

    public HappyPathIntegrationTests(AppHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StartRide_HappyPath_CompletesWithAllPassengersRecorded()
    {
        var httpClient = _fixture.CreateHttpClient("gateway");
        var harness = new RideWorkflowTestHarness(httpClient);
        var rideId = RideCatalog.ThunderMountain.RideId.ToString();

        await harness.StartRideAsync(rideId);

        await harness.WaitForStateAsync(rideId, RideStatus.Running, TimeSpan.FromSeconds(60));
        await harness.WaitForStateAsync(rideId, RideStatus.Completed, TimeSpan.FromSeconds(180));
    }
}
