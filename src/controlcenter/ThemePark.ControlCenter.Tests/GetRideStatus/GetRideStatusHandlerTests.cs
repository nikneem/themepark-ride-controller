using Dapr.Client;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ThemePark.ControlCenter.Features.GetRideStatus;
using ThemePark.Rides.Abstractions.DataTransferObjects;

namespace ThemePark.ControlCenter.Tests.GetRideStatus;

public sealed class GetRideStatusHandlerTests
{
    private readonly DaprClient _daprClient = Substitute.For<DaprClient>();
    private readonly GetRideStatusHandler _handler;

    public GetRideStatusHandlerTests()
    {
        _handler = new GetRideStatusHandler(_daprClient, NullLogger<GetRideStatusHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_RideFound_ReturnsStatusResponse()
    {
        var rideId = Guid.NewGuid();
        var dto = new RideStateDto(rideId, "Test Ride", "Running", 20, 10, null);
        _daprClient
            .InvokeMethodAsync<RideStateDto>(
                Arg.Any<HttpMethod>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _handler.HandleAsync(new GetRideStatusQuery(rideId.ToString()));

        Assert.NotNull(result);
        Assert.Equal(rideId, result.RideId);
        Assert.Equal("Test Ride", result.Name);
    }

    [Fact]
    public async Task HandleAsync_DaprThrowsException_ReturnsNull()
    {
        _daprClient
            .InvokeMethodAsync<RideStateDto>(
                Arg.Any<HttpMethod>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Dapr unavailable"));

        var result = await _handler.HandleAsync(new GetRideStatusQuery("any-ride-id"));

        Assert.Null(result);
    }
}
