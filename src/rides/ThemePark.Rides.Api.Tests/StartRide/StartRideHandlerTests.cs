using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using ThemePark.Rides.Api._Shared;
using ThemePark.Rides.Api.StartRide;
using ThemePark.Rides.Models;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Api.Tests.StartRide;

public sealed class StartRideHandlerTests
{
    private readonly Mock<IRideStateStore> _store = new();
    private readonly StartRideHandler _handler;

    public StartRideHandlerTests() => _handler = new StartRideHandler(_store.Object);

    [Fact]
    public async Task HandleAsync_RideIsIdle_TransitionsToRunningAndReturns200()
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Space Coaster", RideStatus.Idle, 12, 0, null);
        _store.Setup(s => s.GetAsync(rideId.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(state);

        var result = await _handler.HandleAsync(rideId.ToString());

        Assert.IsType<Ok>(result);
        _store.Verify(s => s.SaveAsync(
            It.Is<RideState>(r => r.OperationalStatus == RideStatus.Running),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_RideNotFound_Returns404()
    {
        _store.Setup(s => s.GetAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((RideState?)null);

        var result = await _handler.HandleAsync("missing");

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task HandleAsync_RideNotIdle_Returns409()
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Space Coaster", RideStatus.Running, 12, 4, null);
        _store.Setup(s => s.GetAsync(rideId.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(state);

        var result = await _handler.HandleAsync(rideId.ToString());

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, statusResult.StatusCode);
        _store.Verify(s => s.SaveAsync(It.IsAny<RideState>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
