namespace ThemePark.EventContracts.Events;

/// <summary>
/// Published by Mascot Service on topic "mascot.cleared" when an operator clears a mascot from a restricted zone.
/// </summary>
public sealed record MascotClearedEvent(
    string MascotId,
    Guid ClearedFromRideId,
    DateTimeOffset ClearedAt);
