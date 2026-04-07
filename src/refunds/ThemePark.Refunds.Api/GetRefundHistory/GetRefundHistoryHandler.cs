using Microsoft.AspNetCore.Http.HttpResults;
using ThemePark.Refunds.Api.State;
using ThemePark.Refunds.Models;

namespace ThemePark.Refunds.Api.GetRefundHistory;

public sealed record GetRefundHistoryResponse(
    Guid RideId,
    IReadOnlyList<RefundBatchSummary> History);

public sealed class GetRefundHistoryHandler(IRefundStateStore stateStore)
{
    public async Task<Ok<GetRefundHistoryResponse>> HandleAsync(
        Guid rideId,
        CancellationToken ct = default)
    {
        var history = await stateStore.GetHistoryAsync(rideId, ct);
        return TypedResults.Ok(new GetRefundHistoryResponse(rideId, history));
    }
}
