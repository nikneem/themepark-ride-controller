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

    // Well-known ride GUIDs for each restricted zone
    public static readonly Guid RideAId = new("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid RideBId = new("b0000000-0000-0000-0000-000000000002");
    public static readonly Guid RideCId = new("c0000000-0000-0000-0000-000000000003");

    private static readonly Dictionary<string, Guid> ZoneToRide = new(StringComparer.OrdinalIgnoreCase)
    {
        [ZoneA] = RideAId,
        [ZoneB] = RideBId,
        [ZoneC] = RideCId,
    };

    // Mapping from targetRideId slug (e.g. "ride-zone-a") to zone name
    private static readonly Dictionary<string, string> RideIdToZone = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ride-zone-a"] = ZoneA,
        ["ride-zone-b"] = ZoneB,
        ["ride-zone-c"] = ZoneC,
    };

    public static bool IsRestrictedZone(string zone) =>
        RestrictedZones.Contains(zone, StringComparer.OrdinalIgnoreCase);

    public static Guid? GetRideId(string zone) =>
        ZoneToRide.TryGetValue(zone, out var id) ? id : null;

    /// <summary>Maps a targetRideId slug to its corresponding zone name, or null if unknown.</summary>
    public static string? GetZoneForRideId(string targetRideId) =>
        RideIdToZone.TryGetValue(targetRideId, out var zone) ? zone : null;
}
