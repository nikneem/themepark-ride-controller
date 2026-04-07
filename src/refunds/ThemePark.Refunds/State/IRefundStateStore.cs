using ThemePark.Refunds.Models;

namespace ThemePark.Refunds.State;

public interface IRefundStateStore
{
    Task<RefundBatch?> GetBatchAsync(Guid refundBatchId, CancellationToken ct = default);
    Task SaveBatchAsync(RefundBatch batch, CancellationToken ct = default);
    Task<IReadOnlyList<RefundBatchSummary>> GetHistoryAsync(Guid rideId, CancellationToken ct = default);
    Task SaveHistoryAsync(Guid rideId, IReadOnlyList<RefundBatchSummary> history, CancellationToken ct = default);
}
