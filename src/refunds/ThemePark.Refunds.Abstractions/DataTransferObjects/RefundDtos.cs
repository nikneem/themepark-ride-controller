namespace ThemePark.Refunds.Abstractions.DataTransferObjects;

public sealed record RefundPassengerRequest(string PassengerId, bool IsVip);

public sealed record IssueRefundRequest(
    Guid RideId,
    string WorkflowId,
    string Reason,
    IReadOnlyList<RefundPassengerRequest> Passengers);

public sealed record IssueRefundResponse(
    Guid RefundBatchId,
    Guid RideId,
    string WorkflowId,
    string Reason,
    int TotalRefunded,
    decimal TotalAmount,
    int VoucherCount,
    DateTimeOffset ProcessedAt);

public sealed record GetRefundHistoryResponse(
    Guid RideId,
    IReadOnlyList<RefundBatchSummaryDto> History);

public sealed record RefundBatchSummaryDto(
    Guid RefundBatchId,
    string WorkflowId,
    string Reason,
    int TotalRefunded,
    decimal TotalAmount,
    int VoucherCount,
    DateTimeOffset ProcessedAt);
