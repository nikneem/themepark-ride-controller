using ThemePark.Shared.Domain;

namespace ThemePark.Mascots.Zones;

public static class MascotZones
{
    public const string ParkCentral = "Park-Central";
    public const string ZoneA = "Zone-A";
    public const string ZoneB = "Zone-B";
    public const string ZoneC = "Zone-C";
    public const string Backstage = "Backstage";

    public static readonly string[] AllZones = [ParkCentral, ZoneA, ZoneB, ZoneC, Backstage];
    public static readonly string[] RestrictedZones = [ZoneA, ZoneB, ZoneC];

    // Representative ride ID per zone (used when publishing mascot-in-restricted-zone events)
    private static readonly Dictionary<string, Guid> ZoneToRide = new(StringComparer.OrdinalIgnoreCase)
    {
        [ZoneA] = RideSeedData.ThunderMountain.RideId,
        [ZoneB] = RideSeedData.SplashCanyon.RideId,
        [ZoneC] = RideSeedData.HauntedMansion.RideId,
    };

    // Maps each canonical ride GUID (as string) to its zone — used to resolve the
    // targetRideId sent by the frontend when simulating a mascot intrusion.
    private static readonly Dictionary<string, string> RideIdToZone = new(StringComparer.OrdinalIgnoreCase)
    {
        [RideSeedData.ThunderMountain.RideId.ToString()] = ZoneA,
        [RideSeedData.SpaceCoaster.RideId.ToString()]    = ZoneA,
        [RideSeedData.DragonsLair.RideId.ToString()]     = ZoneA,
        [RideSeedData.SplashCanyon.RideId.ToString()]    = ZoneB,
        [RideSeedData.HauntedMansion.RideId.ToString()]  = ZoneC,
    };

    public static bool IsRestrictedZone(string zone) =>
        RestrictedZones.Contains(zone, StringComparer.OrdinalIgnoreCase);

    public static Guid? GetRideId(string zone) =>
        ZoneToRide.TryGetValue(zone, out var id) ? id : null;

    /// <summary>Maps a targetRideId slug to its corresponding zone name, or null if unknown.</summary>
    public static string? GetZoneForRideId(string targetRideId) =>
        RideIdToZone.TryGetValue(targetRideId, out var zone) ? zone : null;
}
