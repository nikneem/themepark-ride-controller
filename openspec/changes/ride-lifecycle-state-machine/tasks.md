## 1. Domain Model

- [x] 1.1 Add `RideStatus` enum (`Idle`, `PreFlight`, `Loading`, `Running`, `Paused`, `Maintenance`, `Resuming`, `Completed`, `Failed`) to the ThemePark.Rides domain library
- [x] 1.2 Create `InvalidRideTransitionException` in the ThemePark.Rides domain library
- [x] 1.3 Add `RideStatusChanged` domain event record with fields `rideId`, `previousStatus`, `newStatus`, and `transitionedAt`

## 2. RideStateMachine

- [x] 2.1 Create `RideStateMachine` class with a static transition lookup table covering all valid transitions defined in the spec
- [x] 2.2 Implement `Transition(RideStatus target)` method that throws `InvalidRideTransitionException` for disallowed transitions
- [x] 2.3 On valid transition, raise `RideStatusChanged` domain event and update `CurrentStatus`
- [x] 2.4 Expose collected domain events via a read-only list and provide a `ClearEvents()` method

## 3. State Persistence (Ride Service)

- [x] 3.1 Register Dapr state store client in the Ride Service dependency injection container
- [x] 3.2 Implement a `RideStateRepository` (or equivalent) that reads `RideStatus` from key `ride-state-{rideId}`, returning `Idle` when the key is missing
- [x] 3.3 Implement write method that stores the current `RideStatus` to `ride-state-{rideId}` after a successful transition
- [x] 3.4 Update all ride command handlers to read state before constructing `RideStateMachine` and write state after a successful transition

## 4. Workflow Activities

- [x] 4.1 Update the `StartPreFlight` workflow activity to use `RideStateMachine.Transition(PreFlight)`
- [x] 4.2 Update the `StartLoading` workflow activity to use `RideStateMachine.Transition(Loading)`
- [x] 4.3 Update the `StartRun` workflow activity to use `RideStateMachine.Transition(Running)`
- [x] 4.4 Update pause/resume/maintenance/complete/fail workflow activities to call the corresponding `RideStateMachine` transitions
- [x] 4.5 Ensure each activity reads state from Dapr before transitioning and writes state back on success

## 5. Pub/Sub Event Publishing (Control Center)

- [x] 5.1 Register Dapr pub/sub client in the Control Center dependency injection container
- [x] 5.2 Create `RideStatusChangedEvent` DTO record with fields `rideId`, `previousStatus`, `newStatus`, `workflowStep`, and `changedAt`
- [x] 5.3 Implement a `RideStatusEventPublisher` service that publishes `ride.status-changed` to the Dapr pub/sub broker
- [x] 5.4 Call the publisher after every successful state transition within the Control Center workflow or command handler

## 6. SSE Infrastructure (Control Center API)

- [x] 6.1 Add a singleton `Channel<RideStatusChangedEvent>` to the Control Center API dependency injection container
- [x] 6.2 Register a Dapr pub/sub subscriber that listens to `ride.status-changed` and writes received events to the channel
- [x] 6.3 Implement `GET /api/events/stream` SSE endpoint that reads from the channel and streams events to connected clients
- [x] 6.4 Handle `CancellationToken` in the SSE endpoint to detect client disconnects and stop writing to the response stream

## 7. API Surface Updates

- [x] 7.1 Update `GET /api/rides` response model to include the current `RideStatus` field
- [x] 7.2 Add or update `GET /api/rides/{id}/status` endpoint to return the current `RideStatus` read from the Dapr state store

## 8. Unit Tests

- [x] 8.1 Write unit tests for `RideStateMachine` covering every valid transition in the lookup table (all 13 transitions)
- [x] 8.2 Write unit tests for `RideStateMachine` covering representative invalid transitions (at least one per source status)
- [x] 8.3 Write unit tests verifying `RideStatusChanged` event payload (correct `rideId`, `previousStatus`, `newStatus`, `transitionedAt`)
- [x] 8.4 Write unit tests for `RideStateRepository` covering the missing-key-defaults-to-Idle scenario
- [x] 8.5 Write unit tests for `RideStatusEventPublisher` verifying the pub/sub payload shape

## 9. Integration Tests

- [x] 9.1 Write an integration test that executes a full ride session (`Idle → PreFlight → Loading → Running → Completed → Idle`) and asserts each intermediate status is persisted correctly to the Dapr state store
- [x] 9.2 Write an integration test that connects to `GET /api/events/stream`, triggers a state transition, and asserts the SSE message is received with the correct payload
- [x] 9.3 Write an integration test verifying that a client disconnecting from the SSE stream does not prevent subsequent clients from receiving events
