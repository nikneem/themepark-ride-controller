using ThemePark.Maintenance.Models;

namespace ThemePark.Maintenance.Api.State;

public interface IMaintenanceStateStore
{
    Task<MaintenanceRecord?> GetRecordAsync(Guid maintenanceId, CancellationToken ct = default);
    Task SaveRecordAsync(MaintenanceRecord record, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetRideHistoryAsync(Guid rideId, CancellationToken ct = default);
    Task AppendToRideHistoryAsync(Guid rideId, Guid maintenanceId, CancellationToken ct = default);
}
