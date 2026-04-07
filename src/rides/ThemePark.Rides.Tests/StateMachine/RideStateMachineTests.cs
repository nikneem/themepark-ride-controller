using ThemePark.Rides.Exceptions;
using ThemePark.Rides.StateMachine;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Tests.StateMachine;

/// <summary>
/// Tests every valid and representative invalid transition in the RideStateMachine lookup table.
/// </summary>
public sealed class RideStateMachineTests
{
    // ── Valid transitions (task 8.1) ───────────────────────────────────────────

    [Theory]
    [InlineData(RideStatus.Idle,        RideStatus.PreFlight)]
    [InlineData(RideStatus.PreFlight,   RideStatus.Loading)]
    [InlineData(RideStatus.PreFlight,   RideStatus.Failed)]
    [InlineData(RideStatus.Loading,     RideStatus.Running)]
    [InlineData(RideStatus.Running,     RideStatus.Paused)]
    [InlineData(RideStatus.Running,     RideStatus.Maintenance)]
    [InlineData(RideStatus.Running,     RideStatus.Completed)]
    [InlineData(RideStatus.Running,     RideStatus.Failed)]
    [InlineData(RideStatus.Paused,      RideStatus.Running)]
    [InlineData(RideStatus.Paused,      RideStatus.Failed)]
    [InlineData(RideStatus.Maintenance, RideStatus.Resuming)]
    [InlineData(RideStatus.Maintenance, RideStatus.Failed)]
    [InlineData(RideStatus.Resuming,    RideStatus.Running)]
    [InlineData(RideStatus.Resuming,    RideStatus.Failed)]
    [InlineData(RideStatus.Completed,   RideStatus.Idle)]
    [InlineData(RideStatus.Failed,      RideStatus.Idle)]
    public void Transition_ValidTransition_Succeeds(RideStatus from, RideStatus to)
    {
        var machine = new RideStateMachine("ride-1", from);

        machine.Transition(to);

        Assert.Equal(to, machine.CurrentStatus);
    }

    // ── Invalid transitions — one per source status (task 8.2) ────────────────

    [Theory]
    [InlineData(RideStatus.Idle,        RideStatus.Running)]
    [InlineData(RideStatus.PreFlight,   RideStatus.Idle)]
    [InlineData(RideStatus.Loading,     RideStatus.PreFlight)]
    [InlineData(RideStatus.Running,     RideStatus.PreFlight)]
    [InlineData(RideStatus.Paused,      RideStatus.Idle)]
    [InlineData(RideStatus.Maintenance, RideStatus.Running)]
    [InlineData(RideStatus.Resuming,    RideStatus.Paused)]
    [InlineData(RideStatus.Completed,   RideStatus.Running)]
    [InlineData(RideStatus.Failed,      RideStatus.Running)]
    public void Transition_InvalidTransition_ThrowsInvalidRideTransitionException(RideStatus from, RideStatus to)
    {
        var machine = new RideStateMachine("ride-1", from);

        Assert.Throws<InvalidRideTransitionException>(() => machine.Transition(to));
    }

    [Theory]
    [InlineData(RideStatus.Idle,        RideStatus.Running)]
    [InlineData(RideStatus.Running,     RideStatus.PreFlight)]
    [InlineData(RideStatus.Completed,   RideStatus.Running)]
    public void Transition_InvalidTransition_StatusRemainsUnchanged(RideStatus from, RideStatus to)
    {
        var machine = new RideStateMachine("ride-1", from);

        try { machine.Transition(to); } catch (InvalidRideTransitionException) { }

        Assert.Equal(from, machine.CurrentStatus);
    }

    [Fact]
    public void Transition_InvalidTransition_DoesNotRaiseDomainEvent()
    {
        var machine = new RideStateMachine("ride-1", RideStatus.Loading);

        try { machine.Transition(RideStatus.Idle); } catch (InvalidRideTransitionException) { }

        Assert.Empty(machine.DomainEvents);
    }

    // ── Domain event payload (task 8.3) ───────────────────────────────────────

    [Fact]
    public void Transition_Valid_RaisesRideStatusChangedWithCorrectPayload()
    {
        var before = DateTimeOffset.UtcNow;
        var machine = new RideStateMachine("ride-42", RideStatus.Idle);

        machine.Transition(RideStatus.PreFlight);

        var evt = Assert.Single(machine.DomainEvents);
        Assert.Equal("ride-42",          evt.RideId);
        Assert.Equal(RideStatus.Idle,     evt.PreviousStatus);
        Assert.Equal(RideStatus.PreFlight, evt.NewStatus);
        Assert.True(evt.TransitionedAt >= before);
    }

    [Fact]
    public void Transition_MultipleSteps_RaisesOneEventPerTransition()
    {
        var machine = new RideStateMachine("ride-42", RideStatus.Idle);

        machine.Transition(RideStatus.PreFlight);
        machine.Transition(RideStatus.Loading);

        Assert.Equal(2, machine.DomainEvents.Count);
        Assert.Equal(RideStatus.Idle,      machine.DomainEvents[0].PreviousStatus);
        Assert.Equal(RideStatus.PreFlight,  machine.DomainEvents[0].NewStatus);
        Assert.Equal(RideStatus.PreFlight,  machine.DomainEvents[1].PreviousStatus);
        Assert.Equal(RideStatus.Loading,   machine.DomainEvents[1].NewStatus);
    }

    // ── ClearEvents (task 2.4) ────────────────────────────────────────────────

    [Fact]
    public void ClearEvents_RemovesAllCollectedEvents()
    {
        var machine = new RideStateMachine("ride-1", RideStatus.Idle);
        machine.Transition(RideStatus.PreFlight);

        machine.ClearEvents();

        Assert.Empty(machine.DomainEvents);
    }

    [Fact]
    public void DomainEvents_IsReadOnly_CannotBeModifiedExternally()
    {
        var machine = new RideStateMachine("ride-1", RideStatus.Idle);
        var events = machine.DomainEvents;

        // IReadOnlyList does not expose mutation — verify type constraint
        Assert.IsAssignableFrom<IReadOnlyList<ThemePark.Rides.Events.RideStatusChanged>>(events);
    }
}
