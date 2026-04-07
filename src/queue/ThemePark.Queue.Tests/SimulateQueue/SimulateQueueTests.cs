using Dapr.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Moq;
using ThemePark.Queue.Api.Models;
using ThemePark.Queue.Api.SimulateQueue;
using ThemePark.Queue.Models;

namespace ThemePark.Queue.Tests.SimulateQueue;

public sealed class SimulateQueueHandlerTests
{
    private readonly Mock<DaprClient> _dapr = new();
    private readonly SimulateQueueHandler _handler;

    public SimulateQueueHandlerTests()
    {
        _handler = new SimulateQueueHandler(_dapr.Object);
    }

    [Fact]
    public async Task HandleAsync_GeneratesCorrectPassengerCount()
    {
        List<Passenger>? saved = null;
        _dapr.Setup(d => d.SaveStateAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<List<Passenger>>(), It.IsAny<StateOptions?>(),
                It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, List<Passenger>, StateOptions?, IReadOnlyDictionary<string, string>?, CancellationToken>(
                (_, _, passengers, _, _, _) => saved = passengers)
            .Returns(Task.CompletedTask);

        await _handler.HandleAsync("ride-1", new SimulateQueueRequest(Count: 20, VipProbability: 0.2));

        Assert.NotNull(saved);
        Assert.Equal(20, saved!.Count);
    }

    [Fact]
    public async Task HandleAsync_QueueReplacedNotAppended()
    {
        _dapr.Setup(d => d.SaveStateAsync(
                It.IsAny<string>(), "queue-ride-1",
                It.IsAny<List<Passenger>>(), It.IsAny<StateOptions?>(),
                It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _handler.HandleAsync("ride-1", new SimulateQueueRequest(Count: 10));

        // SaveStateAsync called exactly once — replace, not append
        _dapr.Verify(d => d.SaveStateAsync(
            It.IsAny<string>(), "queue-ride-1",
            It.IsAny<List<Passenger>>(), It.IsAny<StateOptions?>(),
            It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AllPassengerIdsAreUnique()
    {
        List<Passenger>? saved = null;
        _dapr.Setup(d => d.SaveStateAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<List<Passenger>>(), It.IsAny<StateOptions?>(),
                It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, List<Passenger>, StateOptions?, IReadOnlyDictionary<string, string>?, CancellationToken>(
                (_, _, passengers, _, _, _) => saved = passengers)
            .Returns(Task.CompletedTask);

        await _handler.HandleAsync("ride-1", new SimulateQueueRequest(Count: 100));

        Assert.NotNull(saved);
        var distinctIds = saved!.Select(p => p.PassengerId).Distinct().Count();
        Assert.Equal(100, distinctIds);
    }
}

public sealed class SimulateQueueEndpointTests
{
    [Fact]
    public void MapSimulateQueue_DemoModeDisabled_DoesNotRegisterRoute()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Dapr:DemoMode"] = "false" })
            .Build();

        var builder = new Mock<IEndpointRouteBuilder>();
        builder.Setup(b => b.ServiceProvider).Returns(Mock.Of<IServiceProvider>());
        builder.Setup(b => b.DataSources).Returns([]);

        // Should return the same builder without adding any routes
        var result = builder.Object.MapSimulateQueue(config);

        Assert.Same(builder.Object, result);
        builder.Verify(b => b.DataSources, Times.Never);
    }
}
