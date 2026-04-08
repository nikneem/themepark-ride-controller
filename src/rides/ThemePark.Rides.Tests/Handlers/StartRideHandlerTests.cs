using NSubstitute;
using ThemePark.Rides.Features.StartRide;
using ThemePark.Rides.Infrastructure;
using ThemePark.Rides.Models;
using ThemePark.Shared;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Tests.Handlers;

public sealed class StartRideHandlerTests
{
    private readonly IRideStateStore _store = Substitute.For<IRideStateStore>();
    private readonly StartRideHandler _handler;

    public StartRideHandlerTests()
    {
        _handler = new StartRideHandler(_store);
    }

    [Fact]
    public async Task HandleAsync_RideIsIdle_ReturnsSuccess()
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Test Ride", RideStatus.Idle, 20, 0, null);
        _store.GetAsync(rideId.ToString(), Arg.Any<CancellationToken>()).Returns(state);

        var result = await _handler.HandleAsync(new StartRideCommand(rideId.ToString()));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_RideNotFound_ReturnsNotFound()
    {
        _store.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((RideState?)null);

        var result = await _handler.HandleAsync(new StartRideCommand("non-existent-id"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.NotFound, result.ErrorKind);
    }

    [Theory]
    [InlineData(RideStatus.Running)]
    [InlineData(RideStatus.Loading)]
    [InlineData(RideStatus.Paused)]
    [InlineData(RideStatus.Maintenance)]
    public async Task HandleAsync_RideNotIdle_ReturnsConflict(RideStatus status)
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Test Ride", status, 20, 10, null);
        _store.GetAsync(rideId.ToString(), Arg.Any<CancellationToken>()).Returns(state);

        var result = await _handler.HandleAsync(new StartRideCommand(rideId.ToString()));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.Conflict, result.ErrorKind);
    }
}
