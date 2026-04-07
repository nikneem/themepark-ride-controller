using ThemePark.Mascots.Data.InMemory;
using ThemePark.Mascots.Zones;

namespace ThemePark.Mascots.Tests.State;

public class MascotStateStoreTests
{
    [Fact]
    public void Initializes_with_three_mascots()
    {
        var store = new InMemoryMascotStateStore();
        Assert.Equal(3, store.GetAll().Count);
    }

    [Fact]
    public void Initializes_all_mascots_in_ParkCentral()
    {
        var store = new InMemoryMascotStateStore();
        Assert.All(store.GetAll(), m =>
        {
            Assert.Equal(MascotZones.ParkCentral, m.CurrentZone);
            Assert.False(m.IsInRestrictedZone);
            Assert.Null(m.AffectedRideId);
        });
    }

    [Fact]
    public void TryUpdateZone_updates_mascot_to_restricted_zone()
    {
        var store = new InMemoryMascotStateStore();
        var result = store.TryUpdateZone("mascot-001", MascotZones.ZoneA, out var updated);

        Assert.True(result);
        Assert.NotNull(updated);
        Assert.Equal(MascotZones.ZoneA, updated.CurrentZone);
        Assert.True(updated.IsInRestrictedZone);
        Assert.Equal(MascotZones.RideAId, updated.AffectedRideId);
    }

    [Fact]
    public void TryUpdateZone_returns_false_for_unknown_mascot()
    {
        var store = new InMemoryMascotStateStore();
        var result = store.TryUpdateZone("unknown-999", MascotZones.ZoneA, out var updated);

        Assert.False(result);
        Assert.Null(updated);
    }

    [Fact]
    public void IsZoneOccupied_returns_true_for_populated_zone()
    {
        var store = new InMemoryMascotStateStore();
        // All start in Park-Central
        Assert.True(store.IsZoneOccupied(MascotZones.ParkCentral));
    }

    [Fact]
    public void IsZoneOccupied_returns_false_for_empty_zone()
    {
        var store = new InMemoryMascotStateStore();
        Assert.False(store.IsZoneOccupied(MascotZones.ZoneA));
    }

    [Fact]
    public void IsZoneOccupiedByOther_false_when_only_self_in_zone()
    {
        var store = new InMemoryMascotStateStore();
        store.TryUpdateZone("mascot-002", MascotZones.ZoneA, out _);
        store.TryUpdateZone("mascot-003", MascotZones.ZoneB, out _);
        // Only mascot-001 remains in Park-Central
        Assert.False(store.IsZoneOccupiedByOther(MascotZones.ParkCentral, "mascot-001"));
    }

    [Fact]
    public void IsZoneOccupiedByOther_true_when_another_mascot_is_in_zone()
    {
        var store = new InMemoryMascotStateStore();
        store.TryUpdateZone("mascot-001", MascotZones.ZoneA, out _);
        // Zone-A now has mascot-001; check from mascot-002's perspective
        Assert.True(store.IsZoneOccupiedByOther(MascotZones.ZoneA, "mascot-002"));
    }

    [Fact]
    public void GetById_returns_null_for_unknown_mascot()
    {
        var store = new InMemoryMascotStateStore();
        Assert.Null(store.GetById("unknown-999"));
    }

    [Fact]
    public void GetById_returns_mascot_for_known_id()
    {
        var store = new InMemoryMascotStateStore();
        var m = store.GetById("mascot-001");
        Assert.NotNull(m);
        Assert.Equal("mascot-001", m.MascotId);
    }
}

