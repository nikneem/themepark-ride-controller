## Why

The Theme Park Ride Controller is a Dapr Workflows demo application. At the heart of every ride session is a lifecycle that progresses through a defined set of states — from idle to running to completion (or failure). Without a formally implemented state machine, ride status is implicit and scattered across workflow activities, making it impossible to enforce valid transitions, emit consistent status-change events, or display accurate real-time state in the Control Center UI.

## What Changes

- Introduce a `RideStatus` enum with all valid states: `Idle`, `PreFlight`, `Loading`, `Running`, `Paused`, `Maintenance`, `Resuming`, `Completed`, `Failed`.
- Implement a `RideStateMachine` domain class that enforces valid transitions and raises `RideStatusChanged` domain events on every transition.
- Integrate the state machine into the `RideWorkflow` Dapr Workflow so every activity call transitions through the state machine rather than setting status ad hoc.
- Persist ride state in the Ride Service using Dapr state store, keyed by `rideId`.
- Publish `ride.status-changed` pub/sub events from the Control Center whenever the state machine transitions.
- Expose current state via the existing `GET /api/rides/{rideId}/status` endpoint.
- **BREAKING**: The `RideOperationalStatus` enum in the Ride Service is replaced by the canonical `RideStatus` enum.

## Capabilities

### New Capabilities

- `ride-state-machine`: The domain-level state machine enforcing valid `RideStatus` transitions and emitting `RideStatusChanged` events.
- `ride-status-persistence`: Persisting and reading ride state from the Dapr state store in the Ride Service.
- `ride-status-events`: Publishing `ride.status-changed` pub/sub events and streaming them to the frontend via the SSE endpoint.

### Modified Capabilities

<!-- No existing spec files found — no delta specs needed -->

## Impact

- **ThemePark.Rides** (domain library + Api): Add `RideStateMachine`, `RideStatus`, `RideStatusChanged`; update state store integration.
- **ThemePark.ControlCenter**: Subscribe to `ride.status-changed`; update `RideWorkflow` to use `RideStateMachine`; push events to SSE stream.
- **ThemePark.ControlCenter.Api**: `GET /api/rides/{rideId}/status` returns `RideStatus` values from the state machine.
- **Frontend**: No API contract change; existing SSE event shape already includes `newStatus`.
- **All `.Tests` projects**: New unit tests for state machine transitions and integration tests for workflow state progression.
