namespace ThemePark.Shared.Enums;

/// <summary>
/// Represents the operational lifecycle state of a ride, as defined in the
/// <c>core-domain-concepts</c> specification.
/// </summary>
public enum RideStatus
{
    /// <summary>The ride is available and waiting for an operator to start a new session.</summary>
    Idle,

    /// <summary>Pre-flight safety checks are in progress before passengers board.</summary>
    PreFlight,

    /// <summary>Passengers are actively boarding the ride.</summary>
    Loading,

    /// <summary>The ride is currently in operation with passengers aboard.</summary>
    Running,

    /// <summary>The ride has been temporarily suspended mid-session.</summary>
    Paused,

    /// <summary>The ride is undergoing maintenance and cannot be started.</summary>
    Maintenance,

    /// <summary>The ride is resuming from a paused state.</summary>
    Resuming,

    /// <summary>The ride session completed successfully.</summary>
    Completed,

    /// <summary>The ride session ended in failure; compensation (refunds) has been triggered.</summary>
    Failed
}
