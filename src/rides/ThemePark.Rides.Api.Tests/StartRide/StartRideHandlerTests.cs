using Moq;
using ThemePark.Rides.Features.StartRide;
using ThemePark.Rides.Infrastructure;
using ThemePark.Rides.Models;
using ThemePark.Shared;
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

        var result = await _handler.HandleAsync(new StartRideCommand(rideId.ToString()));
        _store.Verify(s => s.SaveAsync(
            It.Is<RideState>(r => r.OperationalStatus == RideStatus.Running),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_RideNotFound_Returns404()
    {
        _store.Setup(s => s.GetAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((RideState?)null);

        var result = await _handler.HandleAsync(new StartRideCommand("missing"));

        Assert.Equal(OperationErrorKind.NotFound, result.ErrorKind);
    }

    /// <summary>
    /// Verifies that a ride start is rejected when <see cref="RideStatus"/> is not <c>Idle</c>.
    /// </summary>
    [Theory]
    [InlineData(RideStatus.Running)]
    [InlineData(RideStatus.Loading)]
    [InlineData(RideStatus.Paused)]
    [InlineData(RideStatus.Maintenance)]
    [InlineData(RideStatus.Failed)]
    public async Task StartRide_WhenNotIdle_ReturnsError(RideStatus nonIdleStatus)
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Space Coaster", nonIdleStatus, 12, 4, null);
        _store.Setup(s => s.GetAsync(rideId.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(state);

        var result = await _handler.HandleAsync(new StartRideCommand(rideId.ToString()));

        Assert.Equal(OperationErrorKind.Conflict, result.ErrorKind);
        _store.Verify(s => s.SaveAsync(It.IsAny<RideState>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_RideNotIdle_Returns409()
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Space Coaster", RideStatus.Running, 12, 4, null);
        _store.Setup(s => s.GetAsync(rideId.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(state);

        var result = await _handler.HandleAsync(new StartRideCommand(rideId.ToString()));

        Assert.Equal(OperationErrorKind.Conflict, result.ErrorKind);
        _store.Verify(s => s.SaveAsync(It.IsAny<RideState>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
