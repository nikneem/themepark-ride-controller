using ThemePark.Rides.Infrastructure;
using ThemePark.Rides.StateMachine;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Tests.Integration;

/// <summary>
/// Integration test verifying that a full ride session drives through all lifecycle states
/// and that each intermediate status is correctly persisted via <see cref="IRideStateRepository"/>.
/// Uses an in-memory repository to simulate the Dapr state store.
/// </summary>
public sealed class FullRideSessionTests
{
    [Fact]
    public async Task FullHappyPath_IdleToCompleted_EachStatusPersistedCorrectly()
    {
        var rideId = Guid.NewGuid().ToString();
        var repo = new InMemoryRideStateRepository();

        // Verify initial state defaults to Idle (missing key).
        Assert.Equal(RideStatus.Idle, await repo.GetStatusAsync(rideId));

        // Drive the happy-path sequence: Idle → PreFlight → Loading → Running → Completed → Idle.
        var transitions = new[]
        {
            RideStatus.PreFlight,
            RideStatus.Loading,
            RideStatus.Running,
            RideStatus.Completed,
            RideStatus.Idle,
        };

        var current = RideStatus.Idle;
        foreach (var target in transitions)
        {
            var machine = new RideStateMachine(rideId, current);
            machine.Transition(target);

            await repo.SaveStatusAsync(rideId, machine.CurrentStatus);

            var persisted = await repo.GetStatusAsync(rideId);
            Assert.Equal(target, persisted);

            current = target;
        }
    }

    [Fact]
    public async Task FullHappyPath_DomainEventsRaisedForEveryTransition()
    {
        var rideId = Guid.NewGuid().ToString();
        var repo = new InMemoryRideStateRepository();

        var transitions = new[]
        {
            RideStatus.PreFlight,
            RideStatus.Loading,
            RideStatus.Running,
            RideStatus.Completed,
            RideStatus.Idle,
        };

        var current = RideStatus.Idle;
        foreach (var target in transitions)
        {
            var machine = new RideStateMachine(rideId, current);
            machine.Transition(target);

            // Each step must produce exactly one domain event with the correct payload.
            var evt = Assert.Single(machine.DomainEvents);
            Assert.Equal(rideId, evt.RideId);
            Assert.Equal(current, evt.PreviousStatus);
            Assert.Equal(target, evt.NewStatus);

            await repo.SaveStatusAsync(rideId, machine.CurrentStatus);
            current = target;
        }
    }

    /// <summary>
    /// Lightweight in-memory <see cref="IRideStateRepository"/> used for integration tests.
    /// Simulates the Dapr state store key-value semantics: missing key defaults to <see cref="RideStatus.Idle"/>.
    /// </summary>
    private sealed class InMemoryRideStateRepository : IRideStateRepository
    {
        private readonly Dictionary<string, RideStatus> _store = new();

        public Task<RideStatus> GetStatusAsync(string rideId, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.GetValueOrDefault(rideId, RideStatus.Idle));

        public Task SaveStatusAsync(string rideId, RideStatus status, CancellationToken cancellationToken = default)
        {
            _store[rideId] = status;
            return Task.CompletedTask;
        }
    }
}
