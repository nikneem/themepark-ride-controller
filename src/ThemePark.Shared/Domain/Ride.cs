using ThemePark.Shared.Enums;

namespace ThemePark.Shared.Domain;

/// <summary>
/// Skeleton representation of the <c>Ride</c> aggregate root as defined in the
/// <c>core-domain-concepts</c> specification.
/// <para>
/// The <c>Ride</c> aggregate is the central entity in the system. It owns operational status,
/// the active passenger list, and active chaos events for a running session. The Rides Service
/// is the system of record; all other services reference <see cref="RideId"/> but do not
/// duplicate ride state.
/// </para>
/// </summary>
/// <param name="RideId">
/// Stable, immutable GUID identifying this ride. Corresponds to the canonical seed GUIDs in
/// <see cref="RideSeedData"/>. This value MUST NOT change after initial creation.
/// </param>
/// <param name="Name">Human-readable display name of the ride (e.g. "Thunder Mountain").</param>
/// <param name="Capacity">Maximum number of passengers permitted simultaneously on this ride.</param>
/// <param name="Zone">
/// Physical park zone where this ride resides. Must be one of <c>Zone-A</c>, <c>Zone-B</c>,
/// or <c>Zone-C</c> as enforced by <see cref="Zone"/>.
/// </param>
/// <param name="Status">
/// Current operational lifecycle state of the ride. See <see cref="RideStatus"/> for all
/// valid transitions. A ride must be in <see cref="RideStatus.Idle"/> before a new workflow
/// session can be started.
/// </param>
public sealed record Ride(
    Guid RideId,
    string Name,
    int Capacity,
    string Zone,
    RideStatus Status);
