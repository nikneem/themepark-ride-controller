using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ThemePark.Rides.Infrastructure;
using ThemePark.Rides.Api.Startup;
using ThemePark.Rides.Models;
using ThemePark.Shared.Catalog;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Api.Tests.Startup;

public sealed class RideSeedServiceTests
{
    private readonly Mock<IRideStateStore> _store = new();
    private readonly RideSeedService _service;

    public RideSeedServiceTests()
    {
        _service = new RideSeedService(_store.Object, NullLogger<RideSeedService>.Instance);
    }

    [Fact]
    public async Task StartAsync_AllRidesMissing_SeedsAllFiveRides()
    {
        _store.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((RideState?)null);

        await _service.StartAsync(CancellationToken.None);

        _store.Verify(s => s.SaveAsync(
            It.Is<RideState>(r => r.OperationalStatus == RideStatus.Idle && r.CurrentPassengerCount == 0),
            It.IsAny<CancellationToken>()), Times.Exactly(RideCatalog.All.Count));
    }

    [Fact]
    public async Task StartAsync_RideAlreadyExists_SkipsExistingAndSeedsMissing()
    {
        var existingRide = RideCatalog.All[0];
        var existingState = new RideState(existingRide.RideId, existingRide.Name, RideStatus.Running, existingRide.Capacity, 5, null);

        _store.Setup(s => s.GetAsync(existingRide.RideId.ToString(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(existingState);
        _store.Setup(s => s.GetAsync(It.Is<string>(id => id != existingRide.RideId.ToString()), It.IsAny<CancellationToken>()))
              .ReturnsAsync((RideState?)null);

        await _service.StartAsync(CancellationToken.None);

        _store.Verify(s => s.SaveAsync(It.IsAny<RideState>(), It.IsAny<CancellationToken>()),
            Times.Exactly(RideCatalog.All.Count - 1));
    }
}
