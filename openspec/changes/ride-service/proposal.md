## Why

The Ride Service is the authoritative source for the physical operational state of each theme park ride. Without it, the Control Center workflow has no reliable way to read or mutate ride status via Dapr service invocation, making end-to-end ride lifecycle orchestration impossible.

## What Changes

- Implement `GET /rides/{rideId}` — returns current operational state
- Implement `POST /rides/{rideId}/start` — transitions ride from Idle to Running
- Implement `POST /rides/{rideId}/pause` — pauses a running ride with a reason
- Implement `POST /rides/{rideId}/resume` — resumes a paused ride back to Running
- Implement `POST /rides/{rideId}/stop` — returns ride to Idle
- Implement `POST /rides/{rideId}/simulate-malfunction` — demo-only endpoint that publishes a `ride.malfunction` pub/sub event
- Persist ride state in Dapr state store using key `ride-state-{rideId}`
- Pre-seed 5 rides on startup (Thunder Mountain, Space Coaster, Splash Canyon, Haunted Mansion, Dragon's Lair)
- Introduce `RideStatus` enum (Idle, Running, Paused, Maintenance, Resuming, Completed, Failed)
- Guard `simulate-malfunction` behind feature flag `Dapr:DemoMode` (default: false)

## Capabilities

### New Capabilities

- `ride-state-management`: CRUD for ride operational state — covers all 5 operational endpoints (get, start, pause, resume, stop), state store reads/writes, status transition validation, and 409 conflict responses when preconditions are not met
- `malfunction-simulation`: Demo-only pub/sub trigger — covers the simulate-malfunction endpoint, feature flag gating, and the `ride.malfunction` event payload published to Dapr pub/sub

### Modified Capabilities

*(none)*

## Impact

- `ThemePark.Rides.Api` — all endpoint implementations, startup seeding, Dapr integration
- `ThemePark.Rides` domain library — `RideStatus` enum, `RideState` model, domain logic for status transitions
