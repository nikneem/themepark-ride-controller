using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using ThemePark.Rides.Api._Shared;
using ThemePark.Rides.Api.ResumeRide;
using ThemePark.Rides.Models;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Api.Tests.ResumeRide;

public sealed class ResumeRideHandlerTests
{
    private readonly Mock<IRideStateStore> _store = new();
    private readonly ResumeRideHandler _handler;

    public ResumeRideHandlerTests() => _handler = new ResumeRideHandler(_store.Object);

    [Fact]
    public async Task HandleAsync_RideIsPaused_TransitionsToRunningAndReturns200()
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Splash Canyon", RideStatus.Paused, 20, 10, "Safety check");
        _store.Setup(s => s.GetAsync(rideId.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(state);

        var result = await _handler.HandleAsync(rideId.ToString());

        Assert.IsType<Ok>(result);
        _store.Verify(s => s.SaveAsync(
            It.Is<RideState>(r => r.OperationalStatus == RideStatus.Running && r.PauseReason == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_RideNotPaused_Returns409()
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Splash Canyon", RideStatus.Idle, 20, 0, null);
        _store.Setup(s => s.GetAsync(rideId.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(state);

        var result = await _handler.HandleAsync(rideId.ToString());

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, statusResult.StatusCode);
        _store.Verify(s => s.SaveAsync(It.IsAny<RideState>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
