using Microsoft.AspNetCore.Http.HttpResults;
using ThemePark.Maintenance.Models;
using ThemePark.Maintenance.State;

namespace ThemePark.Maintenance.Api.GetMaintenanceHistory;

public sealed record MaintenanceHistoryItem(
    Guid MaintenanceId,
    Guid RideId,
    string Reason,
    string Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt,
    int? DurationMinutes);

public sealed record GetMaintenanceHistoryResponse(Guid RideId, IReadOnlyList<MaintenanceHistoryItem> History);

public sealed class GetMaintenanceHistoryHandler(IMaintenanceStateStore stateStore)
{
    public async Task<Results<Ok<GetMaintenanceHistoryResponse>, NotFound>> HandleAsync(
        Guid rideId,
        CancellationToken ct = default)
    {
        var ids = await stateStore.GetRideHistoryAsync(rideId, ct);
        if (ids.Count == 0)
            return TypedResults.NotFound();

        var items = new List<MaintenanceHistoryItem>(ids.Count);
        foreach (var id in ids)
        {
            var record = await stateStore.GetRecordAsync(id, ct);
            if (record is null) continue;
            items.Add(Map(record));
        }

        return TypedResults.Ok(new GetMaintenanceHistoryResponse(rideId, items));
    }

    private static MaintenanceHistoryItem Map(MaintenanceRecord r) =>
        new(r.MaintenanceId, r.RideId, r.Reason.ToString(), r.Status.ToString(),
            r.RequestedAt, r.CompletedAt, r.DurationMinutes);
}
