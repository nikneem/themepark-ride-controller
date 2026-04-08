using Dapr.Client;
using Microsoft.Extensions.Configuration;
using Moq;
using ThemePark.Aspire.ServiceDefaults;
using ThemePark.EventContracts.Events;
using ThemePark.Rides.Infrastructure;
using ThemePark.Rides.Features.SimulateMalfunction;
using ThemePark.Rides.Models;
using ThemePark.Shared;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Api.Tests.SimulateMalfunction;

public sealed class SimulateMalfunctionHandlerTests
{
    private readonly Mock<IRideStateStore> _store = new();
    private readonly Mock<DaprClient> _dapr = new();

    private SimulateMalfunctionHandler CreateHandler(bool demoMode) =>
        new(_store.Object, _dapr.Object, BuildConfig(demoMode));

    private static IConfiguration BuildConfig(bool demoMode) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Dapr:DemoMode"] = demoMode.ToString()
            })
            .Build();

    [Fact]
    public async Task HandleAsync_DemoModeDisabled_ReturnsNotFound()
    {
        var handler = CreateHandler(demoMode: false);

        var result = await handler.HandleAsync(new SimulateMalfunctionCommand("any-ride"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.NotFound, result.ErrorKind);
        _store.Verify(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_DemoModeEnabled_RideNotFound_ReturnsNotFound()
    {
        _store.Setup(s => s.GetAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((RideState?)null);
        var handler = CreateHandler(demoMode: true);

        var result = await handler.HandleAsync(new SimulateMalfunctionCommand("missing"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.NotFound, result.ErrorKind);
    }

    [Fact]
    public async Task HandleAsync_DemoModeEnabled_RideFound_PublishesEventAndReturnsSuccess()
    {
        var rideId = Guid.NewGuid();
        var state = new RideState(rideId, "Thunder Mountain", RideStatus.Running, 24, 10, null);
        _store.Setup(s => s.GetAsync(rideId.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(state);
        _dapr.Setup(d => d.PublishEventAsync(
                AspireConstants.DaprComponents.PubSub,
                "ride.malfunction",
                It.IsAny<RideMalfunctionEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(demoMode: true);

        var result = await handler.HandleAsync(new SimulateMalfunctionCommand(rideId.ToString()));

        Assert.True(result.IsSuccess);
        _dapr.Verify(d => d.PublishEventAsync(
            AspireConstants.DaprComponents.PubSub,
            "ride.malfunction",
            It.Is<RideMalfunctionEvent>(e => e.RideId == rideId && e.RideName == "Thunder Mountain"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
