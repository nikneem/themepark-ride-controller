using Dapr.Client;
using ThemePark.Refunds.Models;
using ThemePark.Refunds.State;

namespace ThemePark.Refunds.Data.Dapr;

/// <summary>
/// Dapr state store implementation of <see cref="IRefundStateStore"/>.
/// Persists <see cref="RefundBatch"/> objects and per-ride refund history.
/// </summary>
public sealed class DaprRefundStateStore(DaprClient daprClient) : IRefundStateStore
{
    private const string StoreName = "statestore";

    private static string BatchKey(Guid refundBatchId) => $"refund-batch-{refundBatchId}";
    private static string HistoryKey(Guid rideId) => $"refund-history-{rideId}";

    public async Task<RefundBatch?> GetBatchAsync(Guid refundBatchId, CancellationToken ct = default)
        => await daprClient.GetStateAsync<RefundBatch?>(StoreName, BatchKey(refundBatchId), cancellationToken: ct);

    public async Task SaveBatchAsync(RefundBatch batch, CancellationToken ct = default)
        => await daprClient.SaveStateAsync(StoreName, BatchKey(batch.RefundBatchId), batch, cancellationToken: ct);

    public async Task<IReadOnlyList<RefundBatchSummary>> GetHistoryAsync(Guid rideId, CancellationToken ct = default)
    {
        var list = await daprClient.GetStateAsync<List<RefundBatchSummary>?>(
            StoreName, HistoryKey(rideId), cancellationToken: ct);
        return list ?? [];
    }

    public async Task SaveHistoryAsync(Guid rideId, IReadOnlyList<RefundBatchSummary> history, CancellationToken ct = default)
        => await daprClient.SaveStateAsync(StoreName, HistoryKey(rideId), history, cancellationToken: ct);
}
