## ADDED Requirements

### Requirement: RideStateMachine enforces valid state transitions
The `RideStateMachine` domain class SHALL enforce the allowed state transition table. When a transition is requested that is not in the valid transition table, the machine SHALL throw an `InvalidRideTransitionException`. When a valid transition is requested, the machine SHALL raise a `RideStatusChanged` domain event and update the current status.

#### Scenario: Valid transition is accepted
- **WHEN** `RideStateMachine` is in `Idle` status and `Transition(PreFlight)` is called
- **THEN** the current status becomes `PreFlight` and a `RideStatusChanged` event is raised

#### Scenario: Invalid transition throws exception
- **WHEN** `RideStateMachine` is in `Idle` status and `Transition(Running)` is called
- **THEN** an `InvalidRideTransitionException` is thrown and the current status remains `Idle`

#### Scenario: Invalid transition does not raise domain event
- **WHEN** `RideStateMachine` is in `Loading` status and `Transition(Idle)` is called
- **THEN** an `InvalidRideTransitionException` is thrown and no `RideStatusChanged` event is raised

### Requirement: Transition lookup table completeness
The `RideStateMachine` SHALL implement the following complete transition table and reject any transition not listed:
- `Idle` → `PreFlight`
- `PreFlight` → `Loading`, `Failed`
- `Loading` → `Running`
- `Running` → `Paused`, `Maintenance`, `Completed`, `Failed`
- `Paused` → `Running`, `Failed`
- `Maintenance` → `Resuming`, `Failed`
- `Resuming` → `Running`, `Failed`
- `Completed` → `Idle`
- `Failed` → `Idle`

#### Scenario: All valid forward transitions are accepted
- **WHEN** `RideStateMachine` attempts each transition listed in the lookup table
- **THEN** each transition succeeds without throwing an exception

#### Scenario: Transition back to an unlisted predecessor throws exception
- **WHEN** `RideStateMachine` is in `Running` status and `Transition(PreFlight)` is called
- **THEN** an `InvalidRideTransitionException` is thrown

#### Scenario: Transition from terminal state Completed resets to Idle
- **WHEN** `RideStateMachine` is in `Completed` status and `Transition(Idle)` is called
- **THEN** the current status becomes `Idle`

#### Scenario: Transition from Failed resets to Idle
- **WHEN** `RideStateMachine` is in `Failed` status and `Transition(Idle)` is called
- **THEN** the current status becomes `Idle`

### Requirement: RideStatusChanged domain event payload
On every successful transition the `RideStateMachine` SHALL raise a `RideStatusChanged` domain event containing: `rideId`, `previousStatus`, `newStatus`, and `transitionedAt` (UTC timestamp).

#### Scenario: Domain event contains correct transition data
- **WHEN** `RideStateMachine` for ride `"ride-42"` transitions from `Idle` to `PreFlight`
- **THEN** the raised `RideStatusChanged` event has `rideId = "ride-42"`, `previousStatus = Idle`, `newStatus = PreFlight`, and `transitionedAt` set to the current UTC time

#### Scenario: Multiple transitions raise one event each
- **WHEN** `RideStateMachine` transitions through `Idle → PreFlight → Loading`
- **THEN** exactly two `RideStatusChanged` events are raised, one per transition, each with the correct `previousStatus` and `newStatus`
