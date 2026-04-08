using NSubstitute;
using ThemePark.Rides.Features.PauseRide;
using ThemePark.Rides.Infrastructure;
using ThemePark.Rides.Models;
using ThemePark.Shared;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Tests.Handlers;

public sealed class PauseRideHandlerTests
{
    private readonly IRideStateStore _store = Substitute.For<IRideStateStore>();
    private readonly PauseRideHandler _handler;

    public PauseRideHandlerTests()
    {
        _handler = new PauseRideHandler(_store);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsSuccess()
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Test Ride", RideStatus.Running, 20, 10, null);
        _store.GetAsync(rideId.ToString(), Arg.Any<CancellationToken>()).Returns(state);

        var result = await _handler.HandleAsync(new PauseRideCommand(rideId.ToString(), "Weather alert"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_NoReason_ReturnsBadRequest()
    {
        var result = await _handler.HandleAsync(new PauseRideCommand("any-id", null));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.BadRequest, result.ErrorKind);
    }

    [Fact]
    public async Task HandleAsync_RideNotFound_ReturnsNotFound()
    {
        _store.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((RideState?)null);

        var result = await _handler.HandleAsync(new PauseRideCommand("non-existent-id", "reason"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.NotFound, result.ErrorKind);
    }

    [Theory]
    [InlineData(RideStatus.Idle)]
    [InlineData(RideStatus.Paused)]
    [InlineData(RideStatus.Maintenance)]
    public async Task HandleAsync_RideNotRunning_ReturnsConflict(RideStatus status)
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Test Ride", status, 20, 0, null);
        _store.GetAsync(rideId.ToString(), Arg.Any<CancellationToken>()).Returns(state);

        var result = await _handler.HandleAsync(new PauseRideCommand(rideId.ToString(), "Some reason"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.Conflict, result.ErrorKind);
    }
}
