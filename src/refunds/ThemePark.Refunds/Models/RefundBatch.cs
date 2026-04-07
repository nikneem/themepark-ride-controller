using ThemePark.Shared.Enums;

namespace ThemePark.Refunds.Models;

/// <summary>A single passenger included in a refund batch.</summary>
public sealed record RefundPassenger(string PassengerId, bool IsVip);

/// <summary>Full refund batch record as persisted in the state store.</summary>
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
