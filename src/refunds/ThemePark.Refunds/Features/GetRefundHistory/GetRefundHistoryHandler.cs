using ThemePark.Refunds.Abstractions.DataTransferObjects;
using ThemePark.Refunds.State;
using ThemePark.Shared;

namespace ThemePark.Refunds.Features.GetRefundHistory;

public sealed class GetRefundHistoryHandler(IRefundStateStore stateStore)
{
    public async Task<OperationResult<GetRefundHistoryResponse>> HandleAsync(
        Guid rideId,
        CancellationToken ct = default)
    {
        var history = await stateStore.GetHistoryAsync(rideId, ct);
        var dtos = history.Select(s => new RefundBatchSummaryDto(
            s.RefundBatchId, s.WorkflowId, s.Reason.ToString(),
            s.TotalRefunded, s.TotalAmount, s.VoucherCount, s.ProcessedAt)).ToList();

        return OperationResult<GetRefundHistoryResponse>.Success(
            new GetRefundHistoryResponse(rideId, dtos));
    }
}
