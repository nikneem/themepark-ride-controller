using Moq;
using ThemePark.Rides.Features.StopRide;
using ThemePark.Rides.Infrastructure;
using ThemePark.Rides.Models;
using ThemePark.Shared;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Api.Tests.StopRide;

public sealed class StopRideHandlerTests
{
    private readonly Mock<IRideStateStore> _store = new();
    private readonly StopRideHandler _handler;

    public StopRideHandlerTests() => _handler = new StopRideHandler(_store.Object);

    [Fact]
    public async Task HandleAsync_RideIsRunning_TransitionsToIdleAndReturns200()
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Dragon's Lair", RideStatus.Running, 8, 6, null);
        _store.Setup(s => s.GetAsync(rideId.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(state);

        var result = await _handler.HandleAsync(new StopRideCommand(rideId.ToString()));

        Assert.True(result.IsSuccess);
        _store.Verify(s => s.SaveAsync(
            It.Is<RideState>(r => r.OperationalStatus == RideStatus.Idle),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_RideInMaintenance_Returns409()
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Dragon's Lair", RideStatus.Maintenance, 8, 0, null);
        _store.Setup(s => s.GetAsync(rideId.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(state);

        var result = await _handler.HandleAsync(new StopRideCommand(rideId.ToString()));

        Assert.Equal(OperationErrorKind.Conflict, result.ErrorKind);
        _store.Verify(s => s.SaveAsync(It.IsAny<RideState>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
