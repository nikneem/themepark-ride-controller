## 1. Project Scaffolding

- [ ] 1.1 Create `src/ThemePark.Maintenance.Api` project with .NET 10 minimal API template and add to solution
- [ ] 1.2 Register `maintenance-service` in the Aspire AppHost with Dapr sidecar, port 5103, state store, and pub/sub component references

## 2. Domain Model

- [ ] 2.1 Add `MaintenanceRecord` domain entity with properties: `MaintenanceId` (Guid), `RideId`, `Reason` (enum: MechanicalFailure, ScheduledCheck, Failure), `Status` (enum: Pending, InProgress, Completed, Cancelled), `WorkflowId`, `RequestedAt`, `CompletedAt` (nullable)
- [ ] 2.2 Add computed property `DurationMinutes` (nullable int) derived from `RequestedAt` and `CompletedAt`

## 3. State Persistence

- [ ] 3.1 Implement Dapr state store helpers: save/load `MaintenanceRecord` under key `maintenance-{maintenanceId}` and save/load ride history list under key `maintenance-history-{rideId}` (capped at 20, most recent first)

## 4. CQRS Handlers

- [ ] 4.1 Implement `CreateMaintenanceRequestCommand` handler: validate input, generate GUID, persist record with status `Pending`, append to ride history list, publish `maintenance.requested` event via Dapr pub/sub
- [ ] 4.2 Implement `CompleteMaintenanceRequestCommand` handler: load record, enforce status-transition rules (409 on terminal state, 404 if not found), set status `Completed` and `completedAt`, persist, publish `maintenance.completed` event (payload: `maintenanceId`, `rideId`, `completedAt`)
- [ ] 4.3 Implement `GetMaintenanceHistoryQuery` handler: load ride history list from state store, resolve each `maintenanceId` to a full record, return ordered list (most recent first)

## 5. Minimal API Endpoints

- [ ] 5.1 Map `POST /maintenance/request` → `CreateMaintenanceRequestCommand`; return 201 with `{ maintenanceId, rideId, status }` on success, 400 on validation failure
- [ ] 5.2 Map `POST /maintenance/{maintenanceId}/complete` → `CompleteMaintenanceRequestCommand`; return 200 on success, 404 or 409 as appropriate
- [ ] 5.3 Map `GET /maintenance/{rideId}/history` → `GetMaintenanceHistoryQuery`; return 200 with array (empty array if no records)

## 6. OpenTelemetry & Observability

- [ ] 6.1 Add OpenTelemetry tracing (ActivitySource) to all three handlers per ADR-0008; ensure Aspire dashboard receives spans

## 7. Tests

- [ ] 7.1 Write xUnit unit tests for `CreateMaintenanceRequestCommand` handler covering: successful creation, missing required fields, invalid reason
- [ ] 7.2 Write xUnit unit tests for `CompleteMaintenanceRequestCommand` handler covering: successful completion, 404 on missing record, 409 on already-completed/cancelled record
- [ ] 7.3 Write xUnit unit tests for `GetMaintenanceHistoryQuery` handler covering: records returned in order, history capped at 20, empty result for unknown ride, `durationMinutes` calculation
