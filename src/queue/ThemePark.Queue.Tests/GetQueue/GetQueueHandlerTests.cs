using Dapr.Client;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Moq;
using ThemePark.Queue.Api.GetQueue;
using ThemePark.Queue.Api.Models;
using ThemePark.Queue.Models;

namespace ThemePark.Queue.Tests.GetQueue;

public sealed class GetQueueHandlerTests
{
    private readonly Mock<DaprClient> _dapr = new();

    private GetQueueHandler CreateHandler(double avgCapacity = 20, double avgDuration = 3)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Queue:AverageLoadCapacity"] = avgCapacity.ToString(),
                ["Queue:AverageRideDurationMinutes"] = avgDuration.ToString()
            })
            .Build();
        return new GetQueueHandler(_dapr.Object, config);
    }

    [Fact]
    public async Task HandleAsync_EmptyQueue_ReturnsZeros()
    {
        _dapr.Setup(d => d.GetStateAsync<List<Passenger>?>(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ConsistencyMode?>(), It.IsAny<IReadOnlyDictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<Passenger>?)null);

        var result = await CreateHandler().HandleAsync("ride-1");

        var ok = Assert.IsType<Ok<QueueStateResponse>>(result);
        Assert.Equal(0, ok.Value!.WaitingCount);
        Assert.False(ok.Value.HasVip);
        Assert.Equal(0, ok.Value.EstimatedWaitMinutes);
    }

    [Fact]
    public async Task HandleAsync_PopulatedQueue_ReturnsCorrectCountsAndEstimate()
    {
        var passengers = new List<Passenger>
        {
            new(Guid.NewGuid(), "Alice", IsVip: true),
            new(Guid.NewGuid(), "Bob", IsVip: false),
            new(Guid.NewGuid(), "Carol", IsVip: false),
            new(Guid.NewGuid(), "Dave", IsVip: false)
        };

        _dapr.Setup(d => d.GetStateAsync<List<Passenger>?>(
                It.IsAny<string>(), "queue-ride-42",
                It.IsAny<ConsistencyMode?>(), It.IsAny<IReadOnlyDictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(passengers);

        // estimatedWait = 4 / 20 * 3 = 0.6
        var result = await CreateHandler(avgCapacity: 20, avgDuration: 3).HandleAsync("ride-42");

        var ok = Assert.IsType<Ok<QueueStateResponse>>(result);
        Assert.Equal(4, ok.Value!.WaitingCount);
        Assert.True(ok.Value.HasVip);
        Assert.Equal(0.6, ok.Value.EstimatedWaitMinutes);
    }

    [Fact]
    public async Task HandleAsync_NonVipQueue_HasVipIsFalse()
    {
        var passengers = new List<Passenger>
        {
            new(Guid.NewGuid(), "Alice", IsVip: false),
            new(Guid.NewGuid(), "Bob", IsVip: false)
        };

        _dapr.Setup(d => d.GetStateAsync<List<Passenger>?>(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ConsistencyMode?>(), It.IsAny<IReadOnlyDictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(passengers);

        var result = await CreateHandler().HandleAsync("ride-1");

        var ok = Assert.IsType<Ok<QueueStateResponse>>(result);
        Assert.False(ok.Value!.HasVip);
    }
}
