using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Moq;
using ThemePark.Queue.Abstractions.DataTransferObjects;
using ThemePark.Queue.Api.SimulateQueue;
using ThemePark.Queue.Features.SimulateQueue;
using ThemePark.Queue.Models;
using ThemePark.Queue.State;

namespace ThemePark.Queue.Tests.SimulateQueue;

public sealed class SimulateQueueHandlerTests
{
    private readonly Mock<IQueueStateStore> _store = new();
    private readonly SimulateQueueHandler _handler;

    public SimulateQueueHandlerTests()
    {
        _handler = new SimulateQueueHandler(_store.Object);
    }

    [Fact]
    public async Task HandleAsync_GeneratesCorrectPassengerCount()
    {
        IReadOnlyList<Passenger>? saved = null;
        _store.Setup(s => s.SavePassengersAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyList<Passenger>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyList<Passenger>, CancellationToken>((_, p, _) => saved = p)
            .Returns(Task.CompletedTask);

        await _handler.HandleAsync("ride-1", new SimulateQueueRequest(Count: 20, VipProbability: 0.2));

        Assert.NotNull(saved);
        Assert.Equal(20, saved!.Count);
    }

    [Fact]
    public async Task HandleAsync_QueueReplacedNotAppended()
    {
        _store.Setup(s => s.SavePassengersAsync(
                "ride-1", It.IsAny<IReadOnlyList<Passenger>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _handler.HandleAsync("ride-1", new SimulateQueueRequest(Count: 10));

        // SavePassengersAsync called exactly once — replace, not append
        _store.Verify(s => s.SavePassengersAsync(
            "ride-1", It.IsAny<IReadOnlyList<Passenger>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AllPassengerIdsAreUnique()
    {
        IReadOnlyList<Passenger>? saved = null;
        _store.Setup(s => s.SavePassengersAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyList<Passenger>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyList<Passenger>, CancellationToken>((_, p, _) => saved = p)
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
