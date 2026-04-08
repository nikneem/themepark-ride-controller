using ThemePark.Shared.Records;

namespace ThemePark.Shared.Domain;

/// <summary>
/// Canonical pre-seeded ride data with stable GUIDs, names, zones, and capacities.
/// <para>
/// <b>WARNING:</b> The GUIDs defined here are permanent identifiers shared across all services.
/// They MUST NEVER be changed. Any modification will break Dapr state store lookups,
/// workflow instance IDs, and cross-service references throughout the system.
/// </para>
/// </summary>
public static class RideSeedData
{
    /// <summary>Thunder Mountain — Zone-A, capacity 24.</summary>
    public static readonly RideInfo ThunderMountain = new(
        new Guid("a1b2c3d4-0001-0000-0000-000000000001"),
        "Thunder Mountain",
        24,
        "Zone-A");

    /// <summary>Space Coaster — Zone-A, capacity 12.</summary>
    public static readonly RideInfo SpaceCoaster = new(
        new Guid("a1b2c3d4-0002-0000-0000-000000000002"),
        "Space Coaster",
        12,
        "Zone-A");

    /// <summary>Splash Canyon — Zone-B, capacity 20.</summary>
    public static readonly RideInfo SplashCanyon = new(
        new Guid("a1b2c3d4-0003-0000-0000-000000000003"),
        "Splash Canyon",
        20,
        "Zone-B");

    /// <summary>Haunted Mansion — Zone-C, capacity 16.</summary>
    public static readonly RideInfo HauntedMansion = new(
        new Guid("a1b2c3d4-0004-0000-0000-000000000004"),
        "Haunted Mansion",
        16,
        "Zone-C");

    /// <summary>Dragon's Lair — Zone-A, capacity 8.</summary>
    public static readonly RideInfo DragonsLair = new(
        new Guid("a1b2c3d4-0005-0000-0000-000000000005"),
        "Dragon's Lair",
        8,
        "Zone-A");

    /// <summary>All 5 canonical rides in stable order.</summary>
    public static readonly IReadOnlyList<RideInfo> All =
        Array.AsReadOnly(new[] { ThunderMountain, SpaceCoaster, SplashCanyon, HauntedMansion, DragonsLair });
}
