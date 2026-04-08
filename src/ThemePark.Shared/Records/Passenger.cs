namespace ThemePark.Shared.Records;

/// <summary>
/// Immutable record representing a guest loaded onto a ride session.
/// <para>
/// <b>Immutability invariant</b> (see <c>core-domain-concepts</c> spec): All fields are set
/// at boarding time inside <c>LoadPassengersActivity</c> and MUST NOT be modified for the
/// duration of the session. This invariant is required for deterministic Dapr Workflow replay
/// and for accurate refund calculation at session end.
/// </para>
/// </summary>
/// <param name="PassengerId">Unique identifier for this passenger.</param>
/// <param name="Name">Display name of the passenger.</param>
/// <param name="IsVip">
/// Whether the passenger holds VIP status at the time of boarding. If <c>true</c>, the passenger
/// receives an ice cream voucher in addition to the standard €10.00 refund when a session fails.
/// This flag MUST NOT be changed after boarding completes.
/// </param>
public sealed record Passenger(string PassengerId, string Name, bool IsVip);
