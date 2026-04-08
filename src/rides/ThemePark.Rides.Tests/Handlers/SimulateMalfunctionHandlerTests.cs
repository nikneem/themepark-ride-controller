using Dapr.Client;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using ThemePark.Rides.Features.SimulateMalfunction;
using ThemePark.Rides.Infrastructure;
using ThemePark.Rides.Models;
using ThemePark.Shared;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Tests.Handlers;

public sealed class SimulateMalfunctionHandlerTests
{
    private readonly IRideStateStore _store = Substitute.For<IRideStateStore>();
    private readonly DaprClient _daprClient = Substitute.For<DaprClient>();

    private static IConfiguration BuildConfig(bool demoMode) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Dapr:DemoMode"] = demoMode.ToString() })
            .Build();

    [Fact]
    public async Task HandleAsync_DemoModeEnabled_RideFound_PublishesEventAndReturnsSuccess()
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Thunder Mountain", RideStatus.Running, 20, 10, null);
        _store.GetAsync(rideId.ToString(), Arg.Any<CancellationToken>()).Returns(state);

        var handler = new SimulateMalfunctionHandler(_store, _daprClient, BuildConfig(true));
        var result = await handler.HandleAsync(new SimulateMalfunctionCommand(rideId.ToString()));

        Assert.True(result.IsSuccess);
        await _daprClient.Received(1).PublishEventAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DemoModeDisabled_ReturnsNotFound()
    {
        var handler = new SimulateMalfunctionHandler(_store, _daprClient, BuildConfig(false));
        var result = await handler.HandleAsync(new SimulateMalfunctionCommand("any-ride-id"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.NotFound, result.ErrorKind);
        await _daprClient.DidNotReceive().PublishEventAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DemoModeEnabled_RideNotFound_ReturnsNotFound()
    {
        _store.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((RideState?)null);

        var handler = new SimulateMalfunctionHandler(_store, _daprClient, BuildConfig(true));
        var result = await handler.HandleAsync(new SimulateMalfunctionCommand("non-existent-id"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.NotFound, result.ErrorKind);
    }
}
