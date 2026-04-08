using ThemePark.Shared.Domain;

namespace ThemePark.Shared.Records;

/// <summary>
/// Lightweight, immutable description of a ride as used in the catalog and seed data.
/// The <see cref="Zone"/> field is validated at construction time via <see cref="Domain.Zone.TryParse"/>
/// so that invalid zone strings are rejected before reaching the state store.
/// </summary>
public sealed record RideInfo(Guid RideId, string Name, int Capacity, string Zone)
{
    /// <inheritdoc cref="RideInfo(Guid, string, int, string)"/>
    public string Zone { get; init; } = ValidateZone(Zone);

    private static string ValidateZone(string zone)
    {
        if (!Domain.Zone.TryParse(zone, out _))
            throw new ArgumentException(
                $"Invalid zone '{zone}'. Valid zones are: Zone-A, Zone-B, Zone-C.",
                nameof(zone));
        return zone;
    }
}
