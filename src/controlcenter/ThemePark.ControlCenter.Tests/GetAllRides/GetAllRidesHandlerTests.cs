using Dapr.Client;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ThemePark.ControlCenter.Features.GetAllRides;
using ThemePark.Rides.Abstractions.DataTransferObjects;

namespace ThemePark.ControlCenter.Tests.GetAllRides;

public sealed class GetAllRidesHandlerTests
{
    private readonly DaprClient _daprClient = Substitute.For<DaprClient>();
    private readonly GetAllRidesHandler _handler;

    public GetAllRidesHandlerTests()
    {
        _handler = new GetAllRidesHandler(_daprClient, NullLogger<GetAllRidesHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_DaprReturnsRides_ReturnsList()
    {
        var rides = new List<RideStateDto>
        {
            new(Guid.NewGuid(), "Ride A", "Running", 20, 10, null),
            new(Guid.NewGuid(), "Ride B", "Idle", 30, 0, null),
        };
        _daprClient
            .InvokeMethodAsync<List<RideStateDto>>(
                Arg.Any<HttpMethod>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(rides);

        var result = await _handler.HandleAsync(new GetAllRidesQuery());

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task HandleAsync_DaprThrowsException_ReturnsEmptyList()
    {
        _daprClient
            .InvokeMethodAsync<List<RideStateDto>>(
                Arg.Any<HttpMethod>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Dapr unavailable"));

        var result = await _handler.HandleAsync(new GetAllRidesQuery());

        Assert.Empty(result);
    }
}
