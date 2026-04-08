using Aspire.Hosting.Testing;
using ThemePark.IntegrationTests.Harness;
using ThemePark.Shared.Catalog;
using ThemePark.Shared.Enums;
using ThemePark.Tests.Shared.Fakers;

namespace ThemePark.IntegrationTests.Scenarios;

[Trait("Category", "Integration")]
public sealed class HappyPathIntegrationTests : IClassFixture<DistributedApplicationFactory<Projects.ThemePark_Aspire_AppHost>>
{
    private readonly DistributedApplicationFactory<Projects.ThemePark_Aspire_AppHost> _factory;

    public HappyPathIntegrationTests(DistributedApplicationFactory<Projects.ThemePark_Aspire_AppHost> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartRide_HappyPath_CompletesWithAllPassengersRecorded()
    {
        await _factory.StartAsync();
        var httpClient = _factory.CreateHttpClient("gateway");
        var harness = new RideWorkflowTestHarness(httpClient);
        var rideId = RideCatalog.ThunderMountain.RideId.ToString();

        await harness.StartRideAsync(rideId);

        await harness.WaitForStateAsync(rideId, RideStatus.Running, TimeSpan.FromSeconds(60));
        await harness.WaitForStateAsync(rideId, RideStatus.Completed, TimeSpan.FromSeconds(180));
    }
}
