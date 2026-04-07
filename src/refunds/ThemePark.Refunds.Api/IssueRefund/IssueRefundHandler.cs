using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using ThemePark.Refunds.Models;
using ThemePark.Refunds.State;
using ThemePark.Shared.Enums;

namespace ThemePark.Refunds.Api.IssueRefund;

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

public sealed class IssueRefundHandler(IRefundStateStore stateStore)
{
    private const decimal RefundAmountPerPassenger = 10.00m;
    private const int HistoryCap = 20;
    private static readonly ActivitySource ActivitySource = new("ThemePark.Refunds.Api");

    public async Task<Results<Ok<IssueRefundResponse>, BadRequest<string>>> HandleAsync(
        IssueRefundRequest request,
        CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("IssueRefund");

        if (request.RideId == Guid.Empty)
            return TypedResults.BadRequest("rideId is required.");

        if (string.IsNullOrWhiteSpace(request.WorkflowId))
            return TypedResults.BadRequest("workflowId is required.");

        if (!Enum.TryParse<RefundReason>(request.Reason, ignoreCase: true, out var reason))
            return TypedResults.BadRequest(
                $"Invalid reason '{request.Reason}'. Valid values: MechanicalFailure, WeatherClosure, OperationalDecision.");

        // Idempotency check: return existing batch if workflowId already processed for this ride
        var history = await stateStore.GetHistoryAsync(request.RideId, ct);
        var existing = history.FirstOrDefault(s => s.WorkflowId == request.WorkflowId);
        if (existing is not null)
        {
            activity?.SetTag("refund.idempotent", true);
            return TypedResults.Ok(new IssueRefundResponse(
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

        await stateStore.SaveBatchAsync(batch, ct);

        var summary = new RefundBatchSummary(
            refundBatchId, request.WorkflowId, reason,
            totalRefunded, totalAmount, voucherCount, processedAt);

        // Prepend to history, cap at 20
        var updatedHistory = new List<RefundBatchSummary>(history.Count + 1) { summary };
        updatedHistory.AddRange(history);
        if (updatedHistory.Count > HistoryCap)
            updatedHistory = updatedHistory.Take(HistoryCap).ToList();

        await stateStore.SaveHistoryAsync(request.RideId, updatedHistory, ct);

        activity?.SetTag("refund.batch_id", refundBatchId);
        activity?.SetTag("refund.total_amount", totalAmount);
        activity?.SetTag("refund.voucher_count", voucherCount);

        return TypedResults.Ok(new IssueRefundResponse(
            refundBatchId, request.RideId, request.WorkflowId,
            reason.ToString(), totalRefunded, totalAmount, voucherCount, processedAt));
    }
}
