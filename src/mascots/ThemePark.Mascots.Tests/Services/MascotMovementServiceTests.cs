using Dapr.Client;
using Microsoft.Extensions.Configuration;
using Moq;
using ThemePark.EventContracts.Events;
using ThemePark.Mascots.Api.Services;
using ThemePark.Mascots.Data.InMemory;
using ThemePark.Mascots.Zones;

namespace ThemePark.Mascots.Tests.Services;

public class MascotMovementServiceTests
{
    private static IConfiguration BuildConfig(int intervalSeconds = 60) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MascotSimulation:IntervalSeconds"] = intervalSeconds.ToString()
            })
            .Build();

    private static MascotMovementService CreateService(
        InMemoryMascotStateStore store,
        Mock<DaprClient>? daprMock = null,
        Func<string[], string>? zonePicker = null)
    {
        daprMock ??= new Mock<DaprClient>();
        var config = BuildConfig();

        return zonePicker is not null
            ? new MascotMovementService(store, daprMock.Object, config, zonePicker)
            : new MascotMovementService(store, daprMock.Object, config);
    }

    [Fact]
    public async Task TickAsync_moves_mascots_to_specified_zones()
    {
        var store = new InMemoryMascotStateStore();
        var zones = new[] { MascotZones.ZoneA, MascotZones.ZoneB, MascotZones.ZoneC };
        var index = 0;
        var service = CreateService(store, zonePicker: _ => zones[index++ % zones.Length]);

        await service.TickAsync();

        // All three mascots should have moved into restricted zones (one per zone)
        var all = store.GetAll();
        Assert.All(all, m => Assert.True(m.IsInRestrictedZone, $"{m.MascotId} should be in a restricted zone"));
        var assignedZones = all.Select(m => m.CurrentZone).ToList();
        Assert.Equal(3, assignedZones.Distinct().Count()); // each in a different zone
    }

    [Fact]
    public async Task TickAsync_skips_mascot_when_target_zone_is_occupied()
    {
        var store = new InMemoryMascotStateStore();
        // Pre-populate: mascot-001 in Zone-A
        store.TryUpdateZone("mascot-001", MascotZones.ZoneA, out _);
        store.TryUpdateZone("mascot-002", MascotZones.ZoneB, out _);
        store.TryUpdateZone("mascot-003", MascotZones.ParkCentral, out _);

        // Zone picker always returns Zone-A → mascot-002 and mascot-003 should be skipped
        var service = CreateService(store, zonePicker: _ => MascotZones.ZoneA);

        await service.TickAsync();

        // mascot-001 can move to Zone-A (IsZoneOccupiedByOther = false for itself)
        // mascot-002 tries Zone-A — occupied by mascot-001 → skips, stays Zone-B
        // mascot-003 tries Zone-A — occupied by mascot-001 → skips, stays Park-Central
        Assert.Equal(MascotZones.ZoneA, store.GetById("mascot-001")!.CurrentZone);
        Assert.Equal(MascotZones.ZoneB, store.GetById("mascot-002")!.CurrentZone);
        Assert.Equal(MascotZones.ParkCentral, store.GetById("mascot-003")!.CurrentZone);
    }

    [Fact]
    public async Task TickAsync_publishes_event_only_for_restricted_zones()
    {
        var store = new InMemoryMascotStateStore();
        var daprMock = new Mock<DaprClient>();
        daprMock.Setup(d => d.PublishEventAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<MascotInRestrictedZoneEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var zones = new[] { MascotZones.ZoneA, MascotZones.ParkCentral, MascotZones.Backstage };
        var index = 0;
        var service = CreateService(store, daprMock, _ => zones[index++ % zones.Length]);

        await service.TickAsync();

        // mascot-001 → Zone-A (restricted) → 1 publish
        // mascot-002 → Park-Central (safe) → no publish
        // mascot-003 → Backstage (safe) → no publish
        daprMock.Verify(
            d => d.PublishEventAsync(
                "themepark-pubsub",
                "mascot.in-restricted-zone",
                It.IsAny<MascotInRestrictedZoneEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TickAsync_does_not_publish_event_for_safe_zones()
    {
        var store = new InMemoryMascotStateStore();
        var daprMock = new Mock<DaprClient>();

        var service = CreateService(store, daprMock, _ => MascotZones.ParkCentral);

        await service.TickAsync();

        daprMock.Verify(
            d => d.PublishEventAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
