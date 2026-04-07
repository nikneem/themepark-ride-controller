## Why

The Queue Service manages passenger queues for each ride, and is a critical dependency of the Dapr workflow's `LoadPassengersActivity`. Without it, the workflow has no passengers to load into ride sessions, making the entire conference demo non-functional.

## What Changes

- New `ThemePark.Queue.Api` microservice with Dapr app-id `queue-service` on port 5102
- `GET /queue/{rideId}` — returns current queue state including VIP presence and estimated wait
- `POST /queue/{rideId}/load` — atomically dequeues up to `capacity` passengers, returning a boarding manifest
- `POST /queue/{rideId}/simulate-queue` — demo-only endpoint that seeds the queue with randomly generated passengers

## Capabilities

### New Capabilities

- `passenger-queue-management`: Queue CRUD and atomic load-dequeue operations for ride sessions
- `queue-simulation`: Demo seeding of queues with configurable passenger count and VIP probability

### Modified Capabilities

## Impact

- New project `ThemePark.Queue.Api` to be added to the solution and registered in the Aspire AppHost
- Dapr state store used for queue persistence (key: `queue-{rideId}`)
- `LoadPassengersActivity` in the workflow service depends on this service via Dapr service invocation
- Feature flag `Dapr:DemoMode` gates the simulate-queue endpoint
