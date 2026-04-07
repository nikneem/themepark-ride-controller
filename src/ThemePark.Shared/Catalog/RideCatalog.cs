using ThemePark.Shared.Records;

namespace ThemePark.Shared.Catalog;

/// <summary>
/// Pre-seeded ride catalog with stable GUIDs for local development and testing.
/// GUIDs are hard-coded and must never be changed — all services depend on these identities.
/// </summary>
public static class RideCatalog
{
    public static readonly RideInfo ThunderMountain = new(
        new Guid("a1b2c3d4-0001-0000-0000-000000000001"),
        "Thunder Mountain",
        24,
        "Zone-A");

    public static readonly RideInfo SpaceCoaster = new(
        new Guid("a1b2c3d4-0002-0000-0000-000000000002"),
        "Space Coaster",
        12,
        "Zone-A");

    public static readonly RideInfo SplashCanyon = new(
        new Guid("a1b2c3d4-0003-0000-0000-000000000003"),
        "Splash Canyon",
        20,
        "Zone-B");

    public static readonly RideInfo HauntedMansion = new(
        new Guid("a1b2c3d4-0004-0000-0000-000000000004"),
        "Haunted Mansion",
        16,
        "Zone-C");

    public static readonly RideInfo DragonsLair = new(
        new Guid("a1b2c3d4-0005-0000-0000-000000000005"),
        "Dragon's Lair",
        8,
        "Zone-A");

    public static readonly IReadOnlyList<RideInfo> All =
        Array.AsReadOnly(new[] { ThunderMountain, SpaceCoaster, SplashCanyon, HauntedMansion, DragonsLair });
}
