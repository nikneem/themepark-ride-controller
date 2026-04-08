using ThemePark.Maintenance.Abstractions.DataTransferObjects;
using ThemePark.Maintenance.Models;
using ThemePark.Maintenance.State;
using ThemePark.Shared;
using ThemePark.Shared.Cqrs;

namespace ThemePark.Maintenance.Features.GetMaintenanceHistory;

public sealed class GetMaintenanceHistoryHandler(IMaintenanceStateStore stateStore)
    : IQueryHandler<GetMaintenanceHistoryQuery, OperationResult<GetMaintenanceHistoryResponse>>
{
    public async Task<OperationResult<GetMaintenanceHistoryResponse>> HandleAsync(
        GetMaintenanceHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var ids = await stateStore.GetRideHistoryAsync(query.RideId, cancellationToken);
        if (ids.Count == 0)
            return OperationResult<GetMaintenanceHistoryResponse>.Success(
                new GetMaintenanceHistoryResponse(query.RideId, []));

        var items = new List<MaintenanceHistoryItem>(ids.Count);
        foreach (var id in ids)
        {
            var record = await stateStore.GetRecordAsync(id, cancellationToken);
            if (record is null) continue;
            items.Add(Map(record));
        }

        return OperationResult<GetMaintenanceHistoryResponse>.Success(
            new GetMaintenanceHistoryResponse(query.RideId, items));
    }

    private static MaintenanceHistoryItem Map(MaintenanceRecord r) =>
        new(r.MaintenanceId, r.RideId, r.Reason.ToString(), r.Status.ToString(),
            r.RequestedAt, r.CompletedAt, r.DurationMinutes);
}
