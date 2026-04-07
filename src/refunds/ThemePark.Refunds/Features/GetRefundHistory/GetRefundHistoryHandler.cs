using ThemePark.Refunds.Abstractions.DataTransferObjects;
using ThemePark.Refunds.State;
using ThemePark.Shared;
using ThemePark.Shared.Cqrs;

namespace ThemePark.Refunds.Features.GetRefundHistory;

public sealed class GetRefundHistoryHandler(IRefundStateStore stateStore)
    : IQueryHandler<GetRefundHistoryQuery, OperationResult<GetRefundHistoryResponse>>
{
    public async Task<OperationResult<GetRefundHistoryResponse>> HandleAsync(
        GetRefundHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var history = await stateStore.GetHistoryAsync(query.RideId, cancellationToken);
        var dtos = history.Select(s => new RefundBatchSummaryDto(
            s.RefundBatchId, s.WorkflowId, s.Reason.ToString(),
            s.TotalRefunded, s.TotalAmount, s.VoucherCount, s.ProcessedAt)).ToList();

        return OperationResult<GetRefundHistoryResponse>.Success(
            new GetRefundHistoryResponse(query.RideId, dtos));
    }
}
