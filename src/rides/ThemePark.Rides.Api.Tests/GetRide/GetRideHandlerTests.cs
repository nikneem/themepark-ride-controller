using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using ThemePark.Rides.Api._Shared;
using ThemePark.Rides.Api.GetRide;
using ThemePark.Rides.Models;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Api.Tests.GetRide;

public sealed class GetRideHandlerTests
{
    private readonly Mock<IRideStateStore> _store = new();
    private readonly GetRideHandler _handler;

    public GetRideHandlerTests() => _handler = new GetRideHandler(_store.Object);

    [Fact]
    public async Task HandleAsync_RideExists_Returns200WithState()
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Thunder Mountain", RideStatus.Running, 24, 12, null);
        _store.Setup(s => s.GetAsync(rideId.ToString(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(state);

        var result = await _handler.HandleAsync(rideId.ToString());

        var ok = Assert.IsType<Ok<RideStateResponse>>(result);
        Assert.Equal(rideId, ok.Value!.RideId);
        Assert.Equal("Thunder Mountain", ok.Value.Name);
        Assert.Equal("Running", ok.Value.OperationalStatus);
    }

    [Fact]
    public async Task HandleAsync_RideNotFound_Returns404()
    {
        _store.Setup(s => s.GetAsync("unknown", It.IsAny<CancellationToken>()))
              .ReturnsAsync((RideState?)null);

        var result = await _handler.HandleAsync("unknown");

        Assert.IsType<NotFound>(result);
    }
}
