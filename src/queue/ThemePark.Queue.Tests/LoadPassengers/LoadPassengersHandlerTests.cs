using Dapr.Client;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using ThemePark.Queue.Api.LoadPassengers;
using ThemePark.Queue.Api.Models;
using ThemePark.Queue.Models;

namespace ThemePark.Queue.Tests.LoadPassengers;

public sealed class LoadPassengersHandlerTests
{
    private readonly Mock<DaprClient> _dapr = new();
    private readonly LoadPassengersHandler _handler;

    public LoadPassengersHandlerTests()
    {
        _handler = new LoadPassengersHandler(_dapr.Object);
    }

    private void SetupQueue(string rideId, List<Passenger> passengers, string etag = "\"1\"")
    {
        _dapr.Setup(d => d.GetStateAndETagAsync<List<Passenger>?>(
                It.IsAny<string>(), $"queue-{rideId}",
                It.IsAny<ConsistencyMode?>(), It.IsAny<IReadOnlyDictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((passengers, etag));

        _dapr.Setup(d => d.TrySaveStateAsync(
                It.IsAny<string>(), $"queue-{rideId}",
                It.IsAny<List<Passenger>>(), It.IsAny<string>(),
                It.IsAny<StateOptions?>(), It.IsAny<IReadOnlyDictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    [Fact]
    public async Task HandleAsync_FullCapacityLoad_LoadsCorrectCount()
    {
        var passengers = Enumerable.Range(1, 6)
            .Select(i => new Passenger(Guid.NewGuid(), $"Passenger {i}", IsVip: false))
            .ToList();
        SetupQueue("ride-1", passengers);

        var result = await _handler.HandleAsync("ride-1", new LoadPassengersRequest(4));

        var ok = Assert.IsType<Ok<LoadPassengersResponse>>(result);
        Assert.Equal(4, ok.Value!.LoadedCount);
        Assert.Equal(2, ok.Value.RemainingInQueue);
        Assert.Equal(4, ok.Value.Passengers.Count);
    }

    [Fact]
    public async Task HandleAsync_FewerPassengersThanCapacity_LoadsAll()
    {
        var passengers = Enumerable.Range(1, 3)
            .Select(i => new Passenger(Guid.NewGuid(), $"Passenger {i}", IsVip: false))
            .ToList();
        SetupQueue("ride-2", passengers);

        var result = await _handler.HandleAsync("ride-2", new LoadPassengersRequest(10));

        var ok = Assert.IsType<Ok<LoadPassengersResponse>>(result);
        Assert.Equal(3, ok.Value!.LoadedCount);
        Assert.Equal(0, ok.Value.RemainingInQueue);
    }

    [Fact]
    public async Task HandleAsync_EmptyQueue_ReturnsZeroLoaded()
    {
        SetupQueue("ride-3", []);

        var result = await _handler.HandleAsync("ride-3", new LoadPassengersRequest(4));

        var ok = Assert.IsType<Ok<LoadPassengersResponse>>(result);
        Assert.Equal(0, ok.Value!.LoadedCount);
        Assert.Equal(0, ok.Value.VipCount);
        Assert.Empty(ok.Value.Passengers);
    }

    [Fact]
    public async Task HandleAsync_VipPassengers_CountsCorrectly()
    {
        var passengers = new List<Passenger>
        {
            new(Guid.NewGuid(), "Alice", IsVip: true),
            new(Guid.NewGuid(), "Bob", IsVip: true),
            new(Guid.NewGuid(), "Carol", IsVip: false),
            new(Guid.NewGuid(), "Dave", IsVip: false)
        };
        SetupQueue("ride-4", passengers);

        var result = await _handler.HandleAsync("ride-4", new LoadPassengersRequest(4));

        var ok = Assert.IsType<Ok<LoadPassengersResponse>>(result);
        Assert.Equal(2, ok.Value!.VipCount);
    }
}
