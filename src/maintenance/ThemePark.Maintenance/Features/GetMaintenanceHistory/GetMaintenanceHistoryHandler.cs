using ThemePark.Maintenance.Abstractions.DataTransferObjects;
using ThemePark.Maintenance.Models;
using ThemePark.Maintenance.State;
using ThemePark.Shared;

namespace ThemePark.Maintenance.Features.GetMaintenanceHistory;

public sealed class GetMaintenanceHistoryHandler(IMaintenanceStateStore stateStore)
{
    public async Task<OperationResult<GetMaintenanceHistoryResponse>> HandleAsync(
        Guid rideId,
        CancellationToken ct = default)
    {
        var ids = await stateStore.GetRideHistoryAsync(rideId, ct);
        if (ids.Count == 0)
            return OperationResult<GetMaintenanceHistoryResponse>.NotFound();

        var items = new List<MaintenanceHistoryItem>(ids.Count);
        foreach (var id in ids)
        {
            var record = await stateStore.GetRecordAsync(id, ct);
            if (record is null) continue;
            items.Add(Map(record));
        }

        return OperationResult<GetMaintenanceHistoryResponse>.Success(
            new GetMaintenanceHistoryResponse(rideId, items));
    }

    private static MaintenanceHistoryItem Map(MaintenanceRecord r) =>
        new(r.MaintenanceId, r.RideId, r.Reason.ToString(), r.Status.ToString(),
            r.RequestedAt, r.CompletedAt, r.DurationMinutes);
}
