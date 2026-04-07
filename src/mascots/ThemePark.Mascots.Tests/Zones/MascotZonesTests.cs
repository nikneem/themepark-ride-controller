using ThemePark.Mascots.Zones;

namespace ThemePark.Mascots.Tests.Zones;

public class MascotZonesTests
{
    [Theory]
    [InlineData(MascotZones.ZoneA, true)]
    [InlineData(MascotZones.ZoneB, true)]
    [InlineData(MascotZones.ZoneC, true)]
    [InlineData(MascotZones.ParkCentral, false)]
    [InlineData(MascotZones.Backstage, false)]
    public void IsRestrictedZone_classifies_correctly(string zone, bool expected)
    {
        Assert.Equal(expected, MascotZones.IsRestrictedZone(zone));
    }

    [Theory]
    [InlineData(MascotZones.ZoneA)]
    [InlineData(MascotZones.ZoneB)]
    [InlineData(MascotZones.ZoneC)]
    public void GetRideId_returns_non_null_for_restricted_zones(string zone)
    {
        Assert.NotNull(MascotZones.GetRideId(zone));
    }

    [Theory]
    [InlineData(MascotZones.ParkCentral)]
    [InlineData(MascotZones.Backstage)]
    public void GetRideId_returns_null_for_safe_zones(string zone)
    {
        Assert.Null(MascotZones.GetRideId(zone));
    }

    [Fact]
    public void GetRideId_returns_correct_guid_for_each_zone()
    {
        Assert.Equal(MascotZones.RideAId, MascotZones.GetRideId(MascotZones.ZoneA));
        Assert.Equal(MascotZones.RideBId, MascotZones.GetRideId(MascotZones.ZoneB));
        Assert.Equal(MascotZones.RideCId, MascotZones.GetRideId(MascotZones.ZoneC));
    }

    [Theory]
    [InlineData("ride-zone-a", MascotZones.ZoneA)]
    [InlineData("ride-zone-b", MascotZones.ZoneB)]
    [InlineData("ride-zone-c", MascotZones.ZoneC)]
    public void GetZoneForRideId_maps_slug_to_zone(string rideId, string expectedZone)
    {
        Assert.Equal(expectedZone, MascotZones.GetZoneForRideId(rideId));
    }

    [Theory]
    [InlineData("ride-zone-unknown")]
    [InlineData("Park-Central")]
    [InlineData("Backstage")]
    [InlineData("")]
    public void GetZoneForRideId_returns_null_for_invalid_inputs(string rideId)
    {
        Assert.Null(MascotZones.GetZoneForRideId(rideId));
    }

    [Fact]
    public void AllZones_contains_five_zones()
    {
        Assert.Equal(5, MascotZones.AllZones.Length);
    }

    [Fact]
    public void RestrictedZones_contains_three_zones()
    {
        Assert.Equal(3, MascotZones.RestrictedZones.Length);
    }
}
