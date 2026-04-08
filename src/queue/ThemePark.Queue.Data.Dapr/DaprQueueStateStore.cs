using Dapr.Client;
using ThemePark.Aspire.ServiceDefaults;
using ThemePark.Queue.Models;
using ThemePark.Queue.State;

namespace ThemePark.Queue.Data.Dapr;

/// <summary>
/// Dapr state store implementation of <see cref="IQueueStateStore"/>.
/// Manages per-ride passenger queues at key <c>queue-{rideId}</c>.
/// </summary>
public sealed class DaprQueueStateStore(DaprClient daprClient) : IQueueStateStore
{
    private const string StoreName = AspireConstants.DaprComponents.StateStore;

    private static string Key(string rideId) => $"queue-{rideId}";

    public async Task<IReadOnlyList<Passenger>> GetPassengersAsync(
        string rideId,
        CancellationToken cancellationToken = default)
    {
        var passengers = await daprClient.GetStateAsync<List<Passenger>?>(
            StoreName, Key(rideId), cancellationToken: cancellationToken);
        return passengers ?? [];
    }

    public Task SavePassengersAsync(
        string rideId,
        IReadOnlyList<Passenger> passengers,
        CancellationToken cancellationToken = default) =>
        daprClient.SaveStateAsync(StoreName, Key(rideId), passengers, cancellationToken: cancellationToken);

    public async Task<(IReadOnlyList<Passenger> Passengers, string ETag)> GetPassengersWithETagAsync(
        string rideId,
        CancellationToken cancellationToken = default)
    {
        var (passengers, etag) = await daprClient.GetStateAndETagAsync<List<Passenger>?>(
            StoreName, Key(rideId), cancellationToken: cancellationToken);
        return (passengers ?? [], etag);
    }

    public Task<bool> TrySavePassengersAsync(
        string rideId,
        IReadOnlyList<Passenger> passengers,
        string eTag,
        CancellationToken cancellationToken = default) =>
        daprClient.TrySaveStateAsync(StoreName, Key(rideId), passengers, eTag, cancellationToken: cancellationToken);
}
