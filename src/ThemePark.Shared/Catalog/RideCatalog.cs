using ThemePark.Shared.Domain;
using ThemePark.Shared.Records;

namespace ThemePark.Shared.Catalog;

/// <summary>
/// Backward-compatible alias for <see cref="RideSeedData"/>.
/// New code should reference <see cref="RideSeedData"/> directly.
/// </summary>
public static class RideCatalog
{
    public static readonly RideInfo ThunderMountain = RideSeedData.ThunderMountain;
    public static readonly RideInfo SpaceCoaster    = RideSeedData.SpaceCoaster;
    public static readonly RideInfo SplashCanyon    = RideSeedData.SplashCanyon;
    public static readonly RideInfo HauntedMansion  = RideSeedData.HauntedMansion;
    public static readonly RideInfo DragonsLair     = RideSeedData.DragonsLair;

    public static readonly IReadOnlyList<RideInfo> All = RideSeedData.All;
}
