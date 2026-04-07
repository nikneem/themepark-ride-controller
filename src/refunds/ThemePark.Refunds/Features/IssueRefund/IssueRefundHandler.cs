using System.Diagnostics;
using ThemePark.Refunds.Abstractions.DataTransferObjects;
using ThemePark.Refunds.Models;
using ThemePark.Refunds.State;
using ThemePark.Shared;
using ThemePark.Shared.Cqrs;
using ThemePark.Shared.Enums;

namespace ThemePark.Refunds.Features.IssueRefund;

public sealed class IssueRefundHandler(IRefundStateStore stateStore)
    : ICommandHandler<IssueRefundRequest, OperationResult<IssueRefundResponse>>
{
    private const decimal RefundAmountPerPassenger = 10.00m;
    private const int HistoryCap = 20;
    private static readonly ActivitySource ActivitySource = new("ThemePark.Refunds");

    public async Task<OperationResult<IssueRefundResponse>> HandleAsync(
        IssueRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("IssueRefund");

        if (request.RideId == Guid.Empty)
            return OperationResult<IssueRefundResponse>.BadRequest("rideId is required.");

        if (string.IsNullOrWhiteSpace(request.WorkflowId))
            return OperationResult<IssueRefundResponse>.BadRequest("workflowId is required.");

        if (!Enum.TryParse<RefundReason>(request.Reason, ignoreCase: true, out var reason))
            return OperationResult<IssueRefundResponse>.BadRequest(
                $"Invalid reason '{request.Reason}'. Valid values: MechanicalFailure, WeatherClosure, OperationalDecision.");

        var history = await stateStore.GetHistoryAsync(request.RideId, cancellationToken);
        var existing = history.FirstOrDefault(s => s.WorkflowId == request.WorkflowId);
        if (existing is not null)
        {
            activity?.SetTag("refund.idempotent", true);
            return OperationResult<IssueRefundResponse>.Success(new IssueRefundResponse(
                existing.RefundBatchId, request.RideId, existing.WorkflowId,
                existing.Reason.ToString(), existing.TotalRefunded,
                existing.TotalAmount, existing.VoucherCount, existing.ProcessedAt));
        }

        var passengers = request.Passengers ?? [];
        var totalRefunded = passengers.Count;
        var totalAmount = totalRefunded * RefundAmountPerPassenger;
        var voucherCount = passengers.Count(p => p.IsVip);
        var processedAt = DateTimeOffset.UtcNow;
        var refundBatchId = Guid.NewGuid();

        var batch = new RefundBatch(
            refundBatchId,
            request.RideId,
            request.WorkflowId,
            reason,
            passengers.Select(p => new RefundPassenger(p.PassengerId, p.IsVip)).ToList(),
            totalRefunded,
            totalAmount,
            voucherCount,
            processedAt);

        await stateStore.SaveBatchAsync(batch, cancellationToken);

        var summary = new RefundBatchSummary(
            refundBatchId, request.WorkflowId, reason,
            totalRefunded, totalAmount, voucherCount, processedAt);

        var updatedHistory = new List<RefundBatchSummary>(history.Count + 1) { summary };
        updatedHistory.AddRange(history);
        if (updatedHistory.Count > HistoryCap)
            updatedHistory = updatedHistory.Take(HistoryCap).ToList();

        await stateStore.SaveHistoryAsync(request.RideId, updatedHistory, cancellationToken);

        activity?.SetTag("refund.batch_id", refundBatchId);
        activity?.SetTag("refund.total_amount", totalAmount);
        activity?.SetTag("refund.voucher_count", voucherCount);

        return OperationResult<IssueRefundResponse>.Success(new IssueRefundResponse(
            refundBatchId, request.RideId, request.WorkflowId,
            reason.ToString(), totalRefunded, totalAmount, voucherCount, processedAt));
    }
}
