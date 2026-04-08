using NSubstitute;
using ThemePark.Rides.Features.GetRide;
using ThemePark.Rides.Infrastructure;
using ThemePark.Rides.Models;
using ThemePark.Shared;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Tests.Handlers;

public sealed class GetRideHandlerTests
{
    private readonly IRideStateStore _store = Substitute.For<IRideStateStore>();
    private readonly GetRideHandler _handler;

    public GetRideHandlerTests()
    {
        _handler = new GetRideHandler(_store);
    }

    [Fact]
    public async Task HandleAsync_RideExists_ReturnsSuccess()
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Test Ride", RideStatus.Idle, 20, 0, null);
        _store.GetAsync(rideId.ToString(), Arg.Any<CancellationToken>()).Returns(state);

        var result = await _handler.HandleAsync(new GetRideQuery(rideId.ToString()));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(rideId, result.Value.RideId);
    }

    [Fact]
    public async Task HandleAsync_RideNotFound_ReturnsNotFound()
    {
        _store.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((RideState?)null);

        var result = await _handler.HandleAsync(new GetRideQuery("non-existent-id"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.NotFound, result.ErrorKind);
    }
}
