namespace ThemePark.Shared.Domain;

/// <summary>
/// Strongly-typed value object representing a physical zone within the theme park.
/// Valid values are <c>Zone-A</c>, <c>Zone-B</c>, and <c>Zone-C</c>.
/// Use <see cref="Parse"/> or <see cref="TryParse"/> to construct; the default constructor
/// produces an invalid (empty) zone and should not be used directly.
/// </summary>
public readonly record struct Zone
{
    private static readonly HashSet<string> ValidValues =
        new(StringComparer.Ordinal) { "Zone-A", "Zone-B", "Zone-C" };

    /// <summary>The string representation of this zone (e.g. <c>Zone-A</c>).</summary>
    public string Value { get; }

    private Zone(string value) => Value = value;

    /// <summary>
    /// Parses <paramref name="value"/> into a <see cref="Zone"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is not one of <c>Zone-A</c>, <c>Zone-B</c>, <c>Zone-C</c>.
    /// </exception>
    public static Zone Parse(string value)
    {
        if (!ValidValues.Contains(value))
            throw new ArgumentException(
                $"'{value}' is not a valid zone. Valid zones are: Zone-A, Zone-B, Zone-C.",
                nameof(value));

        return new Zone(value);
    }

    /// <summary>
    /// Attempts to parse <paramref name="value"/> into a <see cref="Zone"/>.
    /// Returns <c>false</c> (and sets <paramref name="zone"/> to <c>default</c>) for any
    /// value outside <c>Zone-A</c>, <c>Zone-B</c>, <c>Zone-C</c>.
    /// </summary>
    public static bool TryParse(string? value, out Zone zone)
    {
        if (value is not null && ValidValues.Contains(value))
        {
            zone = new Zone(value);
            return true;
        }

        zone = default;
        return false;
    }

    /// <inheritdoc/>
    public override string ToString() => Value ?? string.Empty;

    /// <summary>Implicitly converts a <see cref="Zone"/> to its string representation.</summary>
    public static implicit operator string(Zone zone) => zone.Value ?? string.Empty;
}
