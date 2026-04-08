using ThemePark.Rides.Infrastructure;
using ThemePark.Rides.Models;
using ThemePark.Shared.Domain;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Api.Startup;

/// <summary>
/// Seeds the 5 default rides into the Dapr state store on application startup.
/// Ride data is sourced from <see cref="RideSeedData"/> in <c>ThemePark.Shared</c> — GUIDs
/// are never declared inline in service code.
/// Rides whose key already exists are skipped to preserve operational state across restarts.
/// </summary>
public sealed class RideSeedService(IServiceScopeFactory scopeFactory, ILogger<RideSeedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IRideStateStore>();

        foreach (var info in RideSeedData.All)
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

