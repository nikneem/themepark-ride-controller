namespace ThemePark.Mascots.Models;

public sealed record Mascot(
    string MascotId,
    string Name,
    string CurrentZone,
    bool IsInRestrictedZone,
    Guid? AffectedRideId);
