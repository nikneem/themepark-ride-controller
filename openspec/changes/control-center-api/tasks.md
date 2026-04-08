## 1. Project Setup

- [x] 1.1 Create `ThemePark.ControlCenter.Api` .NET 10 Minimal API project and add to solution
- [x] 1.2 Create `ThemePark.ControlCenter` domain class library and add to solution
- [x] 1.3 Add Dapr SDK, Dapr Workflow, and .NET Aspire service defaults NuGet packages to both projects
- [x] 1.4 Register the API project and its Dapr sidecar in `AppHost/Program.cs`
- [x] 1.5 Add Dapr state store and pub/sub component YAML files under `components/`

## 2. Domain Models

- [x] 2.1 Define `Ride`, `RideSession`, and `ChaosEvent` domain records in `ThemePark.ControlCenter`
- [x] 2.2 Define `RideStatus` and `ChaosEventType` enums
- [x] 2.3 Define `SseEvent` record used for SSE payload serialisation

## 3. Ride Management API — Feature Slices

- [x] 3.1 Implement `GetAllRides` feature slice: query, handler (Dapr service invocation to RideService), and `GET /api/rides` endpoint
- [x] 3.2 Implement `GetRideStatus` feature slice: query, handler, and `GET /api/rides/{rideId}/status` endpoint (404 if not found)
- [x] 3.3 Implement `StartRide` feature slice: command, handler (start Dapr Workflow, store instance ID in state store), and `POST /api/rides/{rideId}/start` endpoint (202/404/409)
- [x] 3.4 Implement `ApproveMaintenance` feature slice: command, handler (raise `MaintenanceApproved` external event), and `POST /api/rides/{rideId}/maintenance/approve` endpoint (202/404)
- [x] 3.5 Implement `ResolveChaosEvent` feature slice: command, handler (raise `MascotCleared`/`WeatherCleared`/`SafetyOverride` external event), and `POST /api/rides/{rideId}/events/{eventId}/resolve` endpoint (202/404)
- [x] 3.6 Implement `GetRideHistory` feature slice: query, handler (last 20 completed sessions from state store), and `GET /api/rides/{rideId}/history` endpoint (200/404)

## 4. Dapr Workflow — RideWorkflow Skeleton

- [x] 4.1 Create `RideWorkflow` class in `Workflows/` folder, register with Dapr Workflow runtime
- [x] 4.2 Implement workflow input/output records (`RideWorkflowInput`, `RideWorkflowOutput`)
- [x] 4.3 Implement pre-flight fan-out: schedule all 4 pre-flight activities in parallel with `Task.WhenAll`
- [x] 4.4 Implement compensation sequence called on any pre-flight failure or timeout
- [x] 4.5 Implement workflow state machine transitions: PreFlight → Riding → PostRide → Terminal
- [x] 4.6 Write state store cleanup activity to delete `active-workflow-{rideId}` on workflow termination

## 5. Dapr Workflow — Activities

- [x] 5.1 Implement `CheckWeatherActivity` (calls WeatherService via Dapr service invocation)
- [x] 5.2 Implement `CheckMascotZoneActivity` (calls MascotService via Dapr service invocation)
- [x] 5.3 Implement `CheckMaintenanceStatusActivity` (calls MaintenanceService via Dapr service invocation)
- [x] 5.4 Implement `CheckSafetySystemsActivity` (calls RideService via Dapr service invocation)
- [x] 5.5 Implement `StartRideActivity` (signals RideService to begin ride)
- [x] 5.6 Implement `StopRideActivity` (signals RideService to stop ride; used in compensation)
- [x] 5.7 Implement `RecordSessionSummaryActivity` (writes completed session summary to state store)

## 6. Workflow — External Event Handling

- [x] 6.1 Implement `WeatherAlertReceived` external event handler in `RideWorkflow`: pause ride, `WaitForExternalEvent<WeatherCleared>` with 10-minute timeout, resume or abort
- [x] 6.2 Implement `MascotIntrusionReceived` external event handler: pause, `WaitForExternalEvent<MascotCleared>` with 5-minute timeout, resume or abort
- [x] 6.3 Implement `MalfunctionReceived` external event handler: pause, `WaitForExternalEvent<MaintenanceApproved>` with 30-minute timeout, then wait for `MaintenanceCompleted`, resume or abort

## 7. Pub/Sub Event Subscriptions

- [x] 7.1 Implement `WeatherAlertSubscription` in `EventSubscriptions/`: subscribe to `weather.alert`, read state store, raise `WeatherAlertReceived` into workflow
- [x] 7.2 Implement `MascotIntrusionSubscription`: subscribe to `mascot.in-restricted-zone`, raise `MascotIntrusionReceived`
- [x] 7.3 Implement `MalfunctionSubscription`: subscribe to `ride.malfunction`, raise `MalfunctionReceived`
- [x] 7.4 Implement `MaintenanceCompletedSubscription`: subscribe to `maintenance.completed`, raise `MaintenanceCompleted`
- [x] 7.5 Add warning-log-and-ack guard in all subscribers for missing active workflow

## 8. SSE Stream

- [x] 8.1 Implement `SseConnectionManager` singleton with `Channel<SseEvent>` per connected client, thread-safe add/remove
- [x] 8.2 Implement `GET /api/events/stream` endpoint: register channel, stream events via async enumerable, remove channel on `RequestAborted`
- [x] 8.3 Implement 15-second heartbeat loop sending `: heartbeat` comment to idle connections
- [x] 8.4 Wire pub/sub subscribers and workflow terminal activities to write `SseEvent` entries to all open channels via `SseConnectionManager`

## 9. Integration Tests

- [x] 9.1 Write xUnit integration tests for `GET /api/rides` and `GET /api/rides/{rideId}/status` using `WebApplicationFactory`
- [x] 9.2 Write xUnit tests for `POST /api/rides/{rideId}/start` covering 202, 404, and 409 responses
- [x] 9.3 Write xUnit unit tests for `RideWorkflow` happy path using Dapr Workflow test host
- [x] 9.4 Write xUnit unit tests for pre-flight failure and compensation path
- [x] 9.5 Write xUnit unit tests for each pub/sub subscriber (mock `DaprClient`, verify `RaiseWorkflowEventAsync` calls)
