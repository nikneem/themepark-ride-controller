using Aspire.Hosting.Testing;
using System.Net.Http.Json;
using ThemePark.IntegrationTests.Harness;
using ThemePark.Shared.Catalog;
using ThemePark.Shared.Enums;
using ThemePark.Tests.Shared.Assertions;
using ThemePark.Tests.Shared.Fakers;

namespace ThemePark.IntegrationTests.Scenarios;

[Trait("Category", "Integration")]
public sealed class SevereWeatherIntegrationTests : IClassFixture<DistributedApplicationFactory<Projects.ThemePark_Aspire_AppHost>>
{
    private readonly DistributedApplicationFactory<Projects.ThemePark_Aspire_AppHost> _factory;

    public SevereWeatherIntegrationTests(DistributedApplicationFactory<Projects.ThemePark_Aspire_AppHost> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartRide_SevereWeatherDuringPreFlight_FailsAndRefundsAllPassengers()
    {
        await _factory.StartAsync();
        var httpClient = _factory.CreateHttpClient("gateway");
        var harness = new RideWorkflowTestHarness(httpClient);
        var injector = new ChaosEventInjector(httpClient);
        var faker = new PassengerFaker();
        var passengers = faker.Generate(20);
        var rideId = RideCatalog.DragonsLair.RideId.ToString();

        await harness.StartRideAsync(rideId);
        await harness.WaitForStateAsync(rideId, RideStatus.PreFlight, TimeSpan.FromSeconds(30));

        await injector.WeatherSevereAsync(rideId);
        await harness.WaitForStateAsync(rideId, RideStatus.Failed, TimeSpan.FromSeconds(30));
    }
}
