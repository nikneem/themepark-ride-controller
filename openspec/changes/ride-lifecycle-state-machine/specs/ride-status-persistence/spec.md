## ADDED Requirements

### Requirement: Ride state stored in Dapr state store
The Ride Service SHALL persist the current `RideStatus` of every ride to the Dapr state store using the key `ride-state-{rideId}`. The stored value SHALL be the string representation of the `RideStatus` enum value.

#### Scenario: State is written after a successful transition
- **WHEN** a command causes a valid state transition for ride `"ride-42"`
- **THEN** the Dapr state store entry with key `ride-state-ride-42` is updated to the new status value

#### Scenario: State is stored using the correct key format
- **WHEN** the Ride Service persists status for ride `"coaster-1"`
- **THEN** the Dapr state store key used is exactly `ride-state-coaster-1`

### Requirement: State read before every command
Before executing any command that can trigger a state transition, the Ride Service SHALL read the current `RideStatus` from the Dapr state store to ensure the in-memory `RideStateMachine` reflects the latest persisted status.

#### Scenario: Command handler reads state before transitioning
- **WHEN** the `StartPreFlightCommand` handler is invoked for ride `"ride-42"`
- **THEN** the handler reads the current status from key `ride-state-ride-42` before calling `RideStateMachine.Transition`

#### Scenario: Stale in-memory status is overwritten by stored status
- **WHEN** the state store holds `PreFlight` for a ride but a handler's local instance holds `Idle`
- **THEN** the handler loads `PreFlight` from the state store and uses that as the starting status for the transition

### Requirement: State written after every successful transition
After a `RideStateMachine` transition succeeds and the `RideStatusChanged` event is raised, the Ride Service SHALL write the new status back to the Dapr state store before returning from the command handler.

#### Scenario: New status is persisted on success
- **WHEN** ride `"ride-42"` transitions from `PreFlight` to `Loading`
- **THEN** the Dapr state store entry `ride-state-ride-42` contains `"Loading"` after the command completes

#### Scenario: State is not written when transition throws
- **WHEN** an `InvalidRideTransitionException` is thrown during a transition attempt
- **THEN** the Dapr state store entry for that ride is not updated

### Requirement: Missing state key defaults to Idle
When the Dapr state store does not contain an entry for a given `ride-state-{rideId}` key, the Ride Service SHALL treat the ride as being in the `Idle` status, enabling rides to be initialised without an explicit setup step.

#### Scenario: First command on a new ride starts from Idle
- **WHEN** no state entry exists for `ride-state-new-ride` and a `StartPreFlightCommand` is issued
- **THEN** the transition from `Idle` to `PreFlight` succeeds and the state is written as `"PreFlight"`

#### Scenario: Missing key does not throw a not-found error
- **WHEN** the Dapr state store returns null for `ride-state-unknown-ride`
- **THEN** the Ride Service resolves the status as `Idle` without throwing an exception
