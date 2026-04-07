using Dapr.Client;
using Moq;
using ThemePark.EventContracts.Events;
using ThemePark.Mascots.Abstractions.DataTransferObjects;
using ThemePark.Mascots.Data.InMemory;
using ThemePark.Mascots.Features.ClearMascot;
using ThemePark.Mascots.Zones;
using ThemePark.Shared;

namespace ThemePark.Mascots.Tests.ClearMascot;

public class ClearMascotHandlerTests
{
    private static (ClearMascotHandler Handler, Mock<DaprClient> Dapr) CreateHandler(InMemoryMascotStateStore store)
    {
        var daprMock = new Mock<DaprClient>();
        daprMock.Setup(d => d.PublishEventAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<MascotClearedEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return (new ClearMascotHandler(store, daprMock.Object), daprMock);
    }

    [Fact]
    public async Task HandleAsync_returns_200_with_clear_details_on_success()
    {
        var store = new InMemoryMascotStateStore();
        store.TryUpdateZone("mascot-001", MascotZones.ZoneA, out _);
        var (handler, dapr) = CreateHandler(store);

        var result = await handler.HandleAsync(new ClearMascotCommand("mascot-001"));

        Assert.True(result.IsSuccess);
        Assert.Equal("mascot-001", result.Value!.MascotId);
        Assert.Equal(MascotZones.RideAId, result.Value.ClearedFromRideId);
    }

    [Fact]
    public async Task HandleAsync_moves_mascot_to_ParkCentral_after_clear()
    {
        var store = new InMemoryMascotStateStore();
        store.TryUpdateZone("mascot-001", MascotZones.ZoneB, out _);
        var (handler, _) = CreateHandler(store);

        await handler.HandleAsync(new ClearMascotCommand("mascot-001"));

        Assert.Equal(MascotZones.ParkCentral, store.GetById("mascot-001")!.CurrentZone);
    }

    [Fact]
    public async Task HandleAsync_publishes_mascot_cleared_event()
    {
        var store = new InMemoryMascotStateStore();
        store.TryUpdateZone("mascot-001", MascotZones.ZoneC, out _);
        var (handler, dapr) = CreateHandler(store);

        await handler.HandleAsync(new ClearMascotCommand("mascot-001"));

        dapr.Verify(
            d => d.PublishEventAsync(
                "themepark-pubsub", "mascot.cleared",
                It.IsAny<MascotClearedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_returns_404_for_unknown_mascot()
    {
        var store = new InMemoryMascotStateStore();
        var (handler, _) = CreateHandler(store);

        var result = await handler.HandleAsync(new ClearMascotCommand("unknown-999"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.NotFound, result.ErrorKind);
    }

    [Fact]
    public async Task HandleAsync_returns_404_when_mascot_not_in_restricted_zone()
    {
        var store = new InMemoryMascotStateStore();
        var (handler, _) = CreateHandler(store);

        var result = await handler.HandleAsync(new ClearMascotCommand("mascot-001"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.NotFound, result.ErrorKind);
    }

    [Fact]
    public async Task HandleAsync_returns_404_when_mascot_in_backstage()
    {
        var store = new InMemoryMascotStateStore();
        store.TryUpdateZone("mascot-002", MascotZones.Backstage, out _);
        var (handler, _) = CreateHandler(store);

        var result = await handler.HandleAsync(new ClearMascotCommand("mascot-002"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.NotFound, result.ErrorKind);
    }
}


