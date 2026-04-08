using ThemePark.Shared.Enums;

namespace ThemePark.Refunds.Models;

/// <summary>A single passenger included in a refund batch.</summary>
public sealed record RefundPassenger(string PassengerId, bool IsVip);

/// <summary>
/// Full refund batch record as persisted in the state store.
/// <para>
/// <b>Idempotency invariant</b> (see <c>domain-invariants</c> spec): Processing a refund
/// request for a given <see cref="WorkflowId"/> more than once MUST produce exactly the same
/// outcome as processing it once. A second invocation for the same <see cref="WorkflowId"/>
/// MUST NOT create duplicate refund records. Callers should check for an existing batch
/// with matching <see cref="WorkflowId"/> before persisting a new one and return the
/// existing result unchanged if found.
/// </para>
/// </summary>
public sealed record RefundBatch(
    Guid RefundBatchId,
    Guid RideId,
    string WorkflowId,
    RefundReason Reason,
    IReadOnlyList<RefundPassenger> Passengers,
    int TotalRefunded,
    decimal TotalAmount,
    int VoucherCount,
    DateTimeOffset ProcessedAt);

/// <summary>Lightweight summary stored in the ride history list (capped at 20).</summary>
public sealed record RefundBatchSummary(
    Guid RefundBatchId,
    string WorkflowId,
    RefundReason Reason,
    int TotalRefunded,
    decimal TotalAmount,
    int VoucherCount,
    DateTimeOffset ProcessedAt);
