using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using ThemePark.Rides.Api._Shared;
using ThemePark.Rides.Api.PauseRide;
using ThemePark.Rides.Models;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Api.Tests.PauseRide;

public sealed class PauseRideHandlerTests
{
    private readonly Mock<IRideStateStore> _store = new();
    private readonly PauseRideHandler _handler;

    public PauseRideHandlerTests() => _handler = new PauseRideHandler(_store.Object);

    [Fact]
    public async Task HandleAsync_EmptyReason_Returns400()
    {
        var request = new PauseRideRequest(Reason: "");

        var result = await _handler.HandleAsync("any", request);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_RideNotFound_Returns404()
    {
        _store.Setup(s => s.GetAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((RideState?)null);

        var result = await _handler.HandleAsync("missing", new PauseRideRequest("Safety check"));

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task HandleAsync_RideNotRunning_Returns409()
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Haunted Mansion", RideStatus.Idle, 16, 0, null);
        _store.Setup(s => s.GetAsync(rideId.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(state);

        var result = await _handler.HandleAsync(rideId.ToString(), new PauseRideRequest("Emergency"));

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, statusResult.StatusCode);
        _store.Verify(s => s.SaveAsync(It.IsAny<RideState>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_RideRunning_PausesWithReasonAndReturns200()
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Haunted Mansion", RideStatus.Running, 16, 8, null);
        _store.Setup(s => s.GetAsync(rideId.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(state);

        var result = await _handler.HandleAsync(rideId.ToString(), new PauseRideRequest("Maintenance check"));

        Assert.IsType<Ok>(result);
        _store.Verify(s => s.SaveAsync(
            It.Is<RideState>(r => r.OperationalStatus == RideStatus.Paused && r.PauseReason == "Maintenance check"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
