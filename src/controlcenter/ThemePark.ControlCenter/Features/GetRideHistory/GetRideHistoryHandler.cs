using Dapr.Client;
using Microsoft.Extensions.Logging;
using ThemePark.Aspire.ServiceDefaults;

namespace ThemePark.ControlCenter.Features.GetRideHistory;

/// <summary>
/// Retrieves the last 20 completed session summaries for a ride from the Dapr state store.
/// Session IDs are stored at key "sessions-{rideId}"; each summary is at "session-summary-{sessionId}".
/// </summary>
public sealed class GetRideHistoryHandler(DaprClient daprClient, ILogger<GetRideHistoryHandler> logger)
{
    private const string StoreName = AspireConstants.DaprComponents.StateStore;

    public async Task<IReadOnlyList<RideHistoryEntry>> HandleAsync(
        GetRideHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var sessionIds = await daprClient.GetStateAsync<List<string>?>(
            StoreName,
            $"sessions-{query.RideId}",
            cancellationToken: cancellationToken);

        if (sessionIds is null || sessionIds.Count == 0)
            return [];

        // Read the last 20 sessions (most recent IDs are appended to the end of the list).
        var recentIds = sessionIds.TakeLast(20).Reverse().ToList();

        var entries = new List<RideHistoryEntry>(recentIds.Count);
        foreach (var sessionId in recentIds)
        {
            var entry = await daprClient.GetStateAsync<RideHistoryEntry?>(
                StoreName,
                $"session-summary-{sessionId}",
                cancellationToken: cancellationToken);

            if (entry is not null)
                entries.Add(entry);
            else
                logger.LogWarning("Session summary not found in state store for session {SessionId}.", sessionId);
        }

        return entries;
    }
}
