using NSubstitute;
using ThemePark.Rides.Features.StopRide;
using ThemePark.Rides.Infrastructure;
using ThemePark.Rides.Models;
using ThemePark.Shared;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Tests.Handlers;

public sealed class StopRideHandlerTests
{
    private readonly IRideStateStore _store = Substitute.For<IRideStateStore>();
    private readonly StopRideHandler _handler;

    public StopRideHandlerTests()
    {
        _handler = new StopRideHandler(_store);
    }

    [Theory]
    [InlineData(RideStatus.Running)]
    [InlineData(RideStatus.Paused)]
    [InlineData(RideStatus.Idle)]
    [InlineData(RideStatus.Loading)]
    public async Task HandleAsync_RideIsNotInMaintenance_ReturnsSuccess(RideStatus status)
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Test Ride", status, 20, 10, null);
        _store.GetAsync(rideId.ToString(), Arg.Any<CancellationToken>()).Returns(state);

        var result = await _handler.HandleAsync(new StopRideCommand(rideId.ToString()));

        Assert.True(result.IsSuccess);
        await _store.Received(1).SaveAsync(Arg.Any<RideState>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RideNotFound_ReturnsNotFound()
    {
        _store.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((RideState?)null);

        var result = await _handler.HandleAsync(new StopRideCommand("non-existent-id"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.NotFound, result.ErrorKind);
    }

    [Fact]
    public async Task HandleAsync_RideInMaintenance_ReturnsConflict()
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Test Ride", RideStatus.Maintenance, 20, 0, null);
        _store.GetAsync(rideId.ToString(), Arg.Any<CancellationToken>()).Returns(state);

        var result = await _handler.HandleAsync(new StopRideCommand(rideId.ToString()));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.Conflict, result.ErrorKind);
    }
}
