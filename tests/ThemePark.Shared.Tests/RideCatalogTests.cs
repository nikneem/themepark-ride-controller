using ThemePark.Shared.Catalog;

namespace ThemePark.Shared.Tests;

public class RideCatalogTests
{
    [Fact]
    public void All_ContainsExactlyFiveRides()
    {
        Assert.Equal(5, RideCatalog.All.Count);
    }

    [Fact]
    public void All_AllGuidsAreDistinct()
    {
        var guids = RideCatalog.All.Select(r => r.RideId).ToHashSet();
        Assert.Equal(5, guids.Count);
    }

    [Fact]
    public void ThunderMountain_HasCorrectProperties()
    {
        Assert.Equal("Thunder Mountain", RideCatalog.ThunderMountain.Name);
        Assert.Equal(24, RideCatalog.ThunderMountain.Capacity);
        Assert.Equal("Zone-A", RideCatalog.ThunderMountain.Zone);
    }

    [Fact]
    public void SpaceCoaster_HasCorrectProperties()
    {
        Assert.Equal("Space Coaster", RideCatalog.SpaceCoaster.Name);
        Assert.Equal(12, RideCatalog.SpaceCoaster.Capacity);
        Assert.Equal("Zone-A", RideCatalog.SpaceCoaster.Zone);
    }

    [Fact]
    public void SplashCanyon_HasCorrectProperties()
    {
        Assert.Equal("Splash Canyon", RideCatalog.SplashCanyon.Name);
        Assert.Equal(20, RideCatalog.SplashCanyon.Capacity);
        Assert.Equal("Zone-B", RideCatalog.SplashCanyon.Zone);
    }

    [Fact]
    public void HauntedMansion_HasCorrectProperties()
    {
        Assert.Equal("Haunted Mansion", RideCatalog.HauntedMansion.Name);
        Assert.Equal(16, RideCatalog.HauntedMansion.Capacity);
        Assert.Equal("Zone-C", RideCatalog.HauntedMansion.Zone);
    }

    [Fact]
    public void DragonsLair_HasCorrectProperties()
    {
        Assert.Equal("Dragon's Lair", RideCatalog.DragonsLair.Name);
        Assert.Equal(8, RideCatalog.DragonsLair.Capacity);
        Assert.Equal("Zone-A", RideCatalog.DragonsLair.Zone);
    }

    [Fact]
    public void All_ZoneAContainsThreeRides()
    {
        var zoneA = RideCatalog.All.Where(r => r.Zone == "Zone-A").ToList();
        Assert.Equal(3, zoneA.Count);
        Assert.Contains(RideCatalog.ThunderMountain, zoneA);
        Assert.Contains(RideCatalog.SpaceCoaster, zoneA);
        Assert.Contains(RideCatalog.DragonsLair, zoneA);
    }

    [Fact]
    public void All_ZoneBContainsOneRide()
    {
        var zoneB = RideCatalog.All.Where(r => r.Zone == "Zone-B").ToList();
        Assert.Single(zoneB);
        Assert.Contains(RideCatalog.SplashCanyon, zoneB);
    }

    [Fact]
    public void All_ZoneCContainsOneRide()
    {
        var zoneC = RideCatalog.All.Where(r => r.Zone == "Zone-C").ToList();
        Assert.Single(zoneC);
        Assert.Contains(RideCatalog.HauntedMansion, zoneC);
    }

    [Fact]
    public void All_MatchesNamedFields()
    {
        Assert.Contains(RideCatalog.ThunderMountain, RideCatalog.All);
        Assert.Contains(RideCatalog.SpaceCoaster, RideCatalog.All);
        Assert.Contains(RideCatalog.SplashCanyon, RideCatalog.All);
        Assert.Contains(RideCatalog.HauntedMansion, RideCatalog.All);
        Assert.Contains(RideCatalog.DragonsLair, RideCatalog.All);
    }

    [Fact]
    public void All_IsReadOnly_ThrowsOnMutation()
    {
        var list = (IList<ThemePark.Shared.Records.RideInfo>)RideCatalog.All;
        Assert.Throws<NotSupportedException>(() => list.Add(RideCatalog.ThunderMountain));
    }
}
