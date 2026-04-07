using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using ThemePark.Rides.Api._Shared;
using ThemePark.Rides.Api.StopRide;
using ThemePark.Rides.Models;
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

        var result = await _handler.HandleAsync(rideId.ToString());

        Assert.IsType<Ok>(result);
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

        var result = await _handler.HandleAsync(rideId.ToString());

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, statusResult.StatusCode);
        _store.Verify(s => s.SaveAsync(It.IsAny<RideState>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
