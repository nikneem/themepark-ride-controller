using Dapr.Client;
using ThemePark.Maintenance.Models;
using ThemePark.Maintenance.State;

namespace ThemePark.Maintenance.Data.Dapr;

/// <summary>
/// Dapr state store implementation of <see cref="IMaintenanceStateStore"/>.
/// Persists <see cref="MaintenanceRecord"/> objects and per-ride history lists.
/// </summary>
public sealed class DaprMaintenanceStateStore(DaprClient daprClient) : IMaintenanceStateStore
{
    private const string StoreName = "statestore";
    private const int HistoryCap = 20;

    private static string RecordKey(Guid maintenanceId) => $"maintenance-{maintenanceId}";
    private static string HistoryKey(Guid rideId) => $"maintenance-history-{rideId}";

    public async Task<MaintenanceRecord?> GetRecordAsync(Guid maintenanceId, CancellationToken ct = default)
    {
        return await daprClient.GetStateAsync<MaintenanceRecord?>(
            StoreName, RecordKey(maintenanceId), cancellationToken: ct);
    }

    public async Task SaveRecordAsync(MaintenanceRecord record, CancellationToken ct = default)
    {
        await daprClient.SaveStateAsync(
            StoreName, RecordKey(record.MaintenanceId), record, cancellationToken: ct);
    }

    public async Task<IReadOnlyList<Guid>> GetRideHistoryAsync(Guid rideId, CancellationToken ct = default)
    {
        var list = await daprClient.GetStateAsync<List<Guid>?>(
            StoreName, HistoryKey(rideId), cancellationToken: ct);
        return list ?? [];
    }

    public async Task AppendToRideHistoryAsync(Guid rideId, Guid maintenanceId, CancellationToken ct = default)
    {
        var (list, etag) = await daprClient.GetStateAndETagAsync<List<Guid>?>(
            StoreName, HistoryKey(rideId), cancellationToken: ct);

        list ??= [];
        list.Insert(0, maintenanceId);

        if (list.Count > HistoryCap)
            list = list.Take(HistoryCap).ToList();

        // Use ETag-based save for optimistic concurrency
        await daprClient.TrySaveStateAsync(
            StoreName, HistoryKey(rideId), list, etag, cancellationToken: ct);
    }
}
