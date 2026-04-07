## Context

The Dapr Workflow for ride-failure recovery (in `ThemePark.RideController`) pauses at `TriggerMaintenanceActivity` and then waits for a `maintenance.completed` pub/sub event before the workflow can resume. Currently no service exists to receive the maintenance request, manage its lifecycle, or publish that event. The Maintenance Service fills this gap.

The service follows the same patterns established by other services in the solution: Vertical Slice Architecture, CQRS handlers (ADR-0004), Minimal APIs (ADR-0005), .NET Aspire orchestration (ADR-0003), and OpenTelemetry tracing (ADR-0008). Dapr is used for state storage and pub/sub, consistent with other services.

## Goals / Non-Goals

**Goals:**
- Provide `POST /maintenance/request`, `POST /maintenance/{id}/complete`, and `GET /maintenance/{rideId}/history` endpoints
- Persist each maintenance record in the Dapr state store
- Maintain a per-ride history list (capped at 20 entries) in the Dapr state store
- Publish `maintenance.requested` and `maintenance.completed` domain events via Dapr pub/sub
- Ensure `maintenance.completed` carries `rideId` so the Control Center can correlate with the correct workflow instance

**Non-Goals:**
- Authentication or authorisation (deferred; demo app)
- Scheduling or recurring maintenance logic
- Notifications to external systems beyond the Dapr pub/sub events already defined
- Any UI for maintenance operators

## Decisions

### Decision 1: maintenanceId is a server-generated GUID

**Rationale**: Callers (the workflow activity) should not need to supply an ID. A server-generated GUID keeps request bodies simple, avoids collisions, and is idiomatic for REST resource creation.  
**Alternative considered**: Caller-supplied ID (idempotency key). Rejected because the workflow already has a `workflowId` that can be stored on the record for correlation; a separate caller-supplied key adds complexity without benefit in this demo.

### Decision 2: State stored in Dapr state store under two keys per record

Each maintenance request is stored under:
- `maintenance-{maintenanceId}` — the full record (id, rideId, reason, status, timestamps, workflowId)
- `maintenance-history-{rideId}` — an ordered list of the last 20 `maintenanceId` values for that ride

**Rationale**: Keeping the full record and the ride-scoped index as separate keys avoids reading the entire history list just to resolve a single record, and fits the key-value model of the Dapr state store. The history list is capped at 20 entries to bound storage growth.  
**Alternative considered**: Single key per ride containing full embedded records. Rejected because large embedded arrays conflict with Dapr's optimistic-concurrency model and make single-record lookups expensive.

### Decision 3: `maintenance.completed` event payload includes `rideId`

The event payload SHALL include `{ maintenanceId, rideId, completedAt }`.

**Rationale**: The Dapr Workflow's `WaitForExternalEvent` is correlated by event name _and_ the ride's workflow instance. Publishing `rideId` allows the Control Center (or any subscriber) to route the event to the correct workflow instance without additional lookups.  
**Alternative considered**: Publish only `maintenanceId` and let subscribers look up the `rideId`. Rejected because it introduces an extra state-store read on the hot path and couples the workflow resume path to the Maintenance Service's availability at resume time.

### Decision 4: CQRS handler pattern for business logic

Commands (`CreateMaintenanceRequestCommand`, `CompleteMaintenanceRequestCommand`) and queries (`GetMaintenanceHistoryQuery`) are implemented as MediatR handlers, consistent with ADR-0004. Minimal API endpoints delegate directly to MediatR, consistent with ADR-0005.

### Decision 5: Status transitions are linear and server-enforced

`Pending → InProgress → Completed` is the happy path. `Cancelled` is a terminal state reachable from any non-terminal state. Attempting to complete an already-completed or cancelled record returns 409 Conflict.

## Risks / Trade-offs

- **Dapr state store concurrency on history list** → The history list key uses Dapr's ETag-based optimistic concurrency. Under high concurrent request rates for the same `rideId`, retries may be needed. Mitigation: Accept last-write-wins for the demo; add retry in the handler.
- **No persistence beyond the Dapr state store** → Data is lost if the Redis (dev) state store is cleared. Mitigation: Acceptable for a conference demo; production would use a durable backend.
- **`maintenance.completed` published before state is fully consistent** → The event is published inside the same handler that writes the completed state. A failure between the state write and the event publish could leave the workflow unblocked while the state still shows `InProgress`. Mitigation: Write state first, then publish; document the at-least-once delivery caveat.
