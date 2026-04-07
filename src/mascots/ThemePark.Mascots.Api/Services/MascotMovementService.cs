using Dapr.Client;
using ThemePark.EventContracts.Events;
using ThemePark.Mascots.Api.State;
using ThemePark.Mascots.Zones;

namespace ThemePark.Mascots.Api.Services;

public sealed class MascotMovementService : IHostedService, IAsyncDisposable
{
    private readonly MascotStateStore _store;
    private readonly DaprClient _daprClient;
    private readonly int _intervalSeconds;
    private readonly Func<string[], string> _zonePicker;
    private PeriodicTimer? _timer;
    private Task? _timerTask;
    private CancellationTokenSource? _cts;

    public MascotMovementService(MascotStateStore store, DaprClient daprClient, IConfiguration configuration)
        : this(store, daprClient, configuration, zones => zones[Random.Shared.Next(zones.Length)])
    {
    }

    public MascotMovementService(MascotStateStore store, DaprClient daprClient, IConfiguration configuration,
        Func<string[], string> zonePicker)
    {
        _store = store;
        _daprClient = daprClient;
        _intervalSeconds = configuration.GetValue<int>("MascotSimulation:IntervalSeconds", 45);
        _zonePicker = zonePicker;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(_intervalSeconds));
        _timerTask = RunAsync(_cts.Token);
        return Task.CompletedTask;
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            while (await _timer!.WaitForNextTickAsync(ct))
                await TickAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
    }

    /// <summary>
    /// Executes one movement tick. Exposed publicly for testing.
    /// </summary>
    public async Task TickAsync(CancellationToken ct = default)
    {
        var zones = MascotZones.AllZones;

        foreach (var mascot in _store.GetAll())
        {
            var targetZone = _zonePicker(zones);

            // Skip if another mascot already occupies the target zone
            if (_store.IsZoneOccupiedByOther(targetZone, mascot.MascotId))
                continue;

            _store.TryUpdateZone(mascot.MascotId, targetZone, out var updated);

            if (updated is { IsInRestrictedZone: true, AffectedRideId: not null })
            {
                var evt = new MascotInRestrictedZoneEvent(
                    Guid.NewGuid(),
                    updated.MascotId,
                    updated.Name,
                    updated.AffectedRideId.Value,
                    DateTimeOffset.UtcNow);

                await _daprClient.PublishEventAsync(
                    "themepark-pubsub", "mascot.in-restricted-zone", evt, ct);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        if (_timerTask is not null)
            await _timerTask.WaitAsync(cancellationToken)
                .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    public ValueTask DisposeAsync()
    {
        _cts?.Dispose();
        _timer?.Dispose();
        return ValueTask.CompletedTask;
    }
}
