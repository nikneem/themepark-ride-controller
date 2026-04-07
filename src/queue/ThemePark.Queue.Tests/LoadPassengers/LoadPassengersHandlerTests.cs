using Moq;
using ThemePark.Queue.Abstractions.DataTransferObjects;
using ThemePark.Queue.Features.LoadPassengers;
using ThemePark.Queue.Models;
using ThemePark.Queue.State;
using ThemePark.Shared;

namespace ThemePark.Queue.Tests.LoadPassengers;

public sealed class LoadPassengersHandlerTests
{
    private readonly Mock<IQueueStateStore> _store = new();
    private readonly LoadPassengersHandler _handler;

    public LoadPassengersHandlerTests()
    {
        _handler = new LoadPassengersHandler(_store.Object);
    }

    private void SetupQueue(string rideId, List<Passenger> passengers, string etag = "\"1\"")
    {
        _store.Setup(s => s.GetPassengersWithETagAsync(rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Passenger>)passengers, etag));

        _store.Setup(s => s.TrySavePassengersAsync(
                rideId, It.IsAny<IReadOnlyList<Passenger>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    [Fact]
    public async Task HandleAsync_FullCapacityLoad_LoadsCorrectCount()
    {
        var passengers = Enumerable.Range(1, 6)
            .Select(i => new Passenger(Guid.NewGuid(), $"Passenger {i}", IsVip: false))
            .ToList();
        SetupQueue("ride-1", passengers);

        var result = await _handler.HandleAsync(new LoadPassengersCommand("ride-1", 4));

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value!.LoadedCount);
        Assert.Equal(2, result.Value.RemainingInQueue);
        Assert.Equal(4, result.Value.Passengers.Count);
    }

    [Fact]
    public async Task HandleAsync_FewerPassengersThanCapacity_LoadsAll()
    {
        var passengers = Enumerable.Range(1, 3)
            .Select(i => new Passenger(Guid.NewGuid(), $"Passenger {i}", IsVip: false))
            .ToList();
        SetupQueue("ride-2", passengers);

        var result = await _handler.HandleAsync(new LoadPassengersCommand("ride-2", 10));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.LoadedCount);
        Assert.Equal(0, result.Value.RemainingInQueue);
    }

    [Fact]
    public async Task HandleAsync_EmptyQueue_ReturnsZeroLoaded()
    {
        SetupQueue("ride-3", []);

        var result = await _handler.HandleAsync(new LoadPassengersCommand("ride-3", 4));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.LoadedCount);
        Assert.Equal(0, result.Value.VipCount);
        Assert.Empty(result.Value.Passengers);
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

        var result = await _handler.HandleAsync(new LoadPassengersCommand("ride-4", 4));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.VipCount);
    }
}
