using ThemePark.Rides.Api._Shared;
using ThemePark.Rides.Models;
using ThemePark.Shared.Catalog;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Api.Startup;

/// <summary>
/// Seeds the 5 default rides into the Dapr state store on application startup.
/// Rides whose key already exists are skipped to preserve operational state across restarts.
/// </summary>
public sealed class RideSeedService(IRideStateStore store, ILogger<RideSeedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var info in RideCatalog.All)
        {
            var rideId = info.RideId.ToString();
            var existing = await store.GetAsync(rideId, cancellationToken);
            if (existing is not null)
            {
                logger.LogInformation("Ride {RideId} ({Name}) already seeded — skipping", info.RideId, info.Name);
                continue;
            }

            var state = new RideState(
                info.RideId,
                info.Name,
                RideStatus.Idle,
                info.Capacity,
                CurrentPassengerCount: 0,
                PauseReason: null);

            await store.SaveAsync(state, cancellationToken);
            logger.LogInformation("Seeded ride {RideId} ({Name}, capacity {Capacity})", info.RideId, info.Name, info.Capacity);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
