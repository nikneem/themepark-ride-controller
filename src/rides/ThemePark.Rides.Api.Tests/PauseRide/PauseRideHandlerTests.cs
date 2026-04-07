using Moq;
using ThemePark.Rides.Features.PauseRide;
using ThemePark.Rides.Infrastructure;
using ThemePark.Rides.Models;
using ThemePark.Shared;
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
        var result = await _handler.HandleAsync(new PauseRideCommand("any", Reason: ""));

        Assert.Equal(OperationErrorKind.BadRequest, result.ErrorKind);
    }

    [Fact]
    public async Task HandleAsync_RideNotFound_Returns404()
    {
        _store.Setup(s => s.GetAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((RideState?)null);

        var result = await _handler.HandleAsync(new PauseRideCommand("missing", "Safety check"));

        Assert.Equal(OperationErrorKind.NotFound, result.ErrorKind);
    }

    [Fact]
    public async Task HandleAsync_RideNotRunning_Returns409()
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Haunted Mansion", RideStatus.Idle, 16, 0, null);
        _store.Setup(s => s.GetAsync(rideId.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(state);

        var result = await _handler.HandleAsync(new PauseRideCommand(rideId.ToString(), "Emergency"));

        Assert.Equal(OperationErrorKind.Conflict, result.ErrorKind);
        _store.Verify(s => s.SaveAsync(It.IsAny<RideState>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_RideRunning_PausesWithReasonAndReturns200()
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Haunted Mansion", RideStatus.Running, 16, 8, null);
        _store.Setup(s => s.GetAsync(rideId.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(state);

        var result = await _handler.HandleAsync(new PauseRideCommand(rideId.ToString(), "Maintenance check"));

        Assert.True(result.IsSuccess);
        _store.Verify(s => s.SaveAsync(
            It.Is<RideState>(r => r.OperationalStatus == RideStatus.Paused && r.PauseReason == "Maintenance check"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
