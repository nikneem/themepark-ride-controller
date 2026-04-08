using Dapr.Client;
using Moq;
using ThemePark.Aspire.ServiceDefaults;
using ThemePark.EventContracts.Events;
using ThemePark.Mascots.Abstractions.DataTransferObjects;
using ThemePark.Mascots.Data.InMemory;
using ThemePark.Mascots.Features.SimulateIntrusion;
using ThemePark.Mascots.Zones;
using ThemePark.Shared;

namespace ThemePark.Mascots.Tests.SimulateIntrusion;

public class SimulateIntrusionHandlerTests
{
    private static (SimulateIntrusionHandler Handler, Mock<DaprClient> Dapr) CreateHandler(InMemoryMascotStateStore store)
    {
        var daprMock = new Mock<DaprClient>();
        daprMock.Setup(d => d.PublishEventAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<MascotInRestrictedZoneEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return (new SimulateIntrusionHandler(store, daprMock.Object), daprMock);
    }

    [Fact]
    public async Task HandleAsync_returns_202_on_success()
    {
        var store = new InMemoryMascotStateStore();
        var (handler, _) = CreateHandler(store);

        var result = await handler.HandleAsync(new SimulateIntrusionRequest("mascot-001", "ride-zone-a"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_updates_mascot_zone_immediately()
    {
        var store = new InMemoryMascotStateStore();
        var (handler, _) = CreateHandler(store);

        await handler.HandleAsync(new SimulateIntrusionRequest("mascot-002", "ride-zone-b"));

        Assert.Equal(MascotZones.ZoneB, store.GetById("mascot-002")!.CurrentZone);
    }

    [Fact]
    public async Task HandleAsync_publishes_mascot_in_restricted_zone_event()
    {
        var store = new InMemoryMascotStateStore();
        var (handler, dapr) = CreateHandler(store);

        await handler.HandleAsync(new SimulateIntrusionRequest("mascot-003", "ride-zone-c"));

        dapr.Verify(
            d => d.PublishEventAsync(
                AspireConstants.DaprComponents.PubSub, "mascot.in-restricted-zone",
                It.IsAny<MascotInRestrictedZoneEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_returns_400_for_unknown_mascot()
    {
        var store = new InMemoryMascotStateStore();
        var (handler, _) = CreateHandler(store);

        var result = await handler.HandleAsync(new SimulateIntrusionRequest("mascot-999", "ride-zone-a"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.BadRequest, result.ErrorKind);
    }

    [Fact]
    public async Task HandleAsync_returns_400_for_unknown_ride_zone()
    {
        var store = new InMemoryMascotStateStore();
        var (handler, _) = CreateHandler(store);

        var result = await handler.HandleAsync(new SimulateIntrusionRequest("mascot-001", "ride-zone-unknown"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.BadRequest, result.ErrorKind);
    }

    [Fact]
    public async Task HandleAsync_returns_400_when_safe_zone_provided_as_target()
    {
        var store = new InMemoryMascotStateStore();
        var (handler, _) = CreateHandler(store);

        var result = await handler.HandleAsync(new SimulateIntrusionRequest("mascot-001", "Park-Central"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.BadRequest, result.ErrorKind);
    }

    [Fact]
    public async Task HandleAsync_does_not_publish_if_zone_update_fails()
    {
        var store = new InMemoryMascotStateStore();
        var daprMock = new Mock<DaprClient>();
        var handler = new SimulateIntrusionHandler(store, daprMock.Object);

        // Trigger with unknown mascot — should bail early
        await handler.HandleAsync(new SimulateIntrusionRequest("unknown", "ride-zone-a"));

        daprMock.Verify(
            d => d.PublishEventAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}


