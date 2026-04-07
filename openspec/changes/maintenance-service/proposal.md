## Why

The Dapr Workflow failure-recovery path pauses and waits for a `maintenance.completed` event before resuming. Without a Maintenance Service to log repair requests, complete them, and publish that event, the entire ride-failure compensation path of the conference demo is broken and cannot be demonstrated end-to-end.

## What Changes

- Introduce a new `ThemePark.Maintenance.Api` microservice (Dapr app-id: `maintenance-service`, port 5103)
- Add `POST /maintenance/request` endpoint to log new maintenance requests
- Add `POST /maintenance/{maintenanceId}/complete` endpoint to mark a request done and publish `maintenance.completed`
- Add `GET /maintenance/{rideId}/history` endpoint to retrieve the last 20 maintenance records for a ride
- Persist maintenance records and ride history in the Dapr state store
- Publish `maintenance.requested` and `maintenance.completed` domain events via Dapr pub/sub

## Capabilities

### New Capabilities

- `maintenance-request-tracking`: Create, complete, and query maintenance records; tracks status transitions (Pending → InProgress → Completed/Cancelled) and persists state via Dapr state store
- `maintenance-events`: Publish `maintenance.requested` on request creation and `maintenance.completed` on completion; the completed event unblocks the Dapr Workflow waiting for repair

### Modified Capabilities

## Impact

- **New service**: `src/ThemePark.Maintenance.Api` wired into the Aspire AppHost and Dapr sidecar
- **Workflow dependency**: `TriggerMaintenanceActivity` in the ride-controller workflow must be able to invoke `POST /maintenance/request` via Dapr service invocation
- **Workflow unblocking**: `maintenance.completed` event (carrying `rideId`) allows the workflow's `WaitForExternalEvent` to resume
- **Infrastructure**: Dapr state store (Redis in dev) and pub/sub component must support the new app-id
- **Tests**: xUnit unit tests for CQRS handlers and integration tests for the minimal API endpoints
