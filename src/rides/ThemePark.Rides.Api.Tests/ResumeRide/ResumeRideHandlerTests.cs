using Moq;
using ThemePark.Rides.Features.ResumeRide;
using ThemePark.Rides.Infrastructure;
using ThemePark.Rides.Models;
using ThemePark.Shared;
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

        Assert.True(result.IsSuccess);
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

        Assert.Equal(OperationErrorKind.Conflict, result.ErrorKind);
        _store.Verify(s => s.SaveAsync(It.IsAny<RideState>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
