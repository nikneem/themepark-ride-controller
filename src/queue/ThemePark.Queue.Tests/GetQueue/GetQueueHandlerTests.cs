using Microsoft.Extensions.Configuration;
using Moq;
using ThemePark.Queue.Abstractions.DataTransferObjects;
using ThemePark.Queue.Features.GetQueue;
using ThemePark.Queue.Models;
using ThemePark.Queue.State;
using ThemePark.Shared;

namespace ThemePark.Queue.Tests.GetQueue;

public sealed class GetQueueHandlerTests
{
    private readonly Mock<IQueueStateStore> _store = new();

    private GetQueueHandler CreateHandler(double avgCapacity = 20, double avgDuration = 3)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Queue:AverageLoadCapacity"] = avgCapacity.ToString(),
                ["Queue:AverageRideDurationMinutes"] = avgDuration.ToString()
            })
            .Build();
        return new GetQueueHandler(_store.Object, config);
    }

    [Fact]
    public async Task HandleAsync_EmptyQueue_ReturnsZeros()
    {
        _store.Setup(s => s.GetPassengersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateHandler().HandleAsync("ride-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.WaitingCount);
        Assert.False(result.Value.HasVip);
        Assert.Equal(0, result.Value.EstimatedWaitMinutes);
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

        _store.Setup(s => s.GetPassengersAsync("ride-42", It.IsAny<CancellationToken>()))
            .ReturnsAsync(passengers);

        // estimatedWait = 4 / 20 * 3 = 0.6
        var result = await CreateHandler(avgCapacity: 20, avgDuration: 3).HandleAsync("ride-42");

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value!.WaitingCount);
        Assert.True(result.Value.HasVip);
        Assert.Equal(0.6, result.Value.EstimatedWaitMinutes);
    }

    [Fact]
    public async Task HandleAsync_NonVipQueue_HasVipIsFalse()
    {
        var passengers = new List<Passenger>
        {
            new(Guid.NewGuid(), "Alice", IsVip: false),
            new(Guid.NewGuid(), "Bob", IsVip: false)
        };

        _store.Setup(s => s.GetPassengersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(passengers);

        var result = await CreateHandler().HandleAsync("ride-1");

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.HasVip);
    }
}
