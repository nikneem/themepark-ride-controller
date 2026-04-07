## 1. Project Setup

- [ ] 1.1 Add `Dapr.Workflow` NuGet package to `ThemePark.ControlCenter`
- [ ] 1.2 Register Dapr Workflow services and all activity/workflow classes in `Program.cs`
- [ ] 1.3 Add `RideWorkflowInput` record with `RideId`, `WorkflowId`, `StartedAt`, `RideDurationSeconds` fields

## 2. Activity Classes

- [ ] 2.1 Implement `CheckRideStatusActivity` — calls `GET /rides/{rideId}` via Dapr service invocation to `ride-service`; throws if status ≠ Idle
- [ ] 2.2 Implement `CheckWeatherActivity` — calls `GET /weather/current` via Dapr service invocation to `weather-service`; throws if severity is Severe
- [ ] 2.3 Implement `CheckMascotActivity` — calls `GET /mascots` via Dapr service invocation to `mascot-service`; throws if any mascot is in the ride zone
- [ ] 2.4 Implement `LoadPassengersActivity` — calls `POST /queue/{rideId}/load` via Dapr service invocation to `queue-service`; returns output including VIP flag
- [ ] 2.5 Implement `StartRideActivity` — calls `POST /rides/{rideId}/start` via Dapr service invocation to `ride-service`
- [ ] 2.6 Implement `PauseRideActivity` — calls `POST /rides/{rideId}/pause` via Dapr service invocation to `ride-service`
- [ ] 2.7 Implement `ResumeRideActivity` — calls `POST /rides/{rideId}/resume` via Dapr service invocation to `ride-service`
- [ ] 2.8 Implement `TriggerMaintenanceActivity` — calls `POST /maintenance/request` via Dapr service invocation to `maintenance-service`
- [ ] 2.9 Implement `IssueRefundActivity` — calls `POST /refunds` via Dapr service invocation to `refund-service`; accepts VIP flag in input
- [ ] 2.10 Implement `CompleteRideActivity` — calls `POST /rides/{rideId}/stop` via Dapr service invocation to `ride-service` and logs completion

## 3. RideWorkflow Orchestrator

- [ ] 3.1 Create `RideWorkflow` class with shared `WorkflowTaskOptions` retry policy (3 attempts, 2s/4s/8s backoff, 30s activity timeout)
- [ ] 3.2 Implement pre-flight sequence: `CheckRideStatusActivity` → fan-out `Task.WhenAll(CheckWeatherActivity, CheckMascotActivity)` → `LoadPassengersActivity` → `StartRideActivity`
- [ ] 3.3 Implement the running loop using `Task.WhenAny` over a `CreateTimer` (configurable duration), `WaitForExternalEventAsync<WeatherAlertEvent>`, `WaitForExternalEventAsync<MascotIntrusionEvent>`, and `WaitForExternalEventAsync<MalfunctionEvent>`
- [ ] 3.4 Implement `WeatherAlertReceived` branch: severity=Mild → Pause → wait `ChaosEventResolved` (10-min timeout → failure path) → Resume → loop; severity=Severe → failure path
- [ ] 3.5 Implement `MascotIntrusionReceived` branch: Pause → wait `ChaosEventResolved` (5-min timeout → auto-resolve) → Resume → loop
- [ ] 3.6 Implement `MalfunctionReceived` branch: `TriggerMaintenanceActivity` → wait `MaintenanceApproved` (30-min timeout → failure path) → wait `ChaosEventResolved` → Resume → loop
- [ ] 3.7 Implement the completion path: timer fires → `CompleteRideActivity` → return `Completed` status
- [ ] 3.8 Implement the failure path: `PauseRideActivity` (if started) → `IssueRefundActivity` (if passengers loaded, with VIP flag) → `TriggerMaintenanceActivity` (if not already called) → `CompleteRideActivity` → return `Failed` status

## 4. API Endpoints

- [ ] 4.1 Add `POST /api/rides/{rideId}/start` Minimal API endpoint — schedules `RideWorkflow` with ID `ride-{rideId}-{yyyyMMddHHmmss}` and returns HTTP 202 with workflow ID
- [ ] 4.2 Add `POST /api/rides/{rideId}/maintenance/approve` endpoint — raises `MaintenanceApproved` event on the workflow instance via `DaprClient.RaiseWorkflowEventAsync`; returns HTTP 202
- [ ] 4.3 Add `POST /api/rides/{rideId}/events/{eventId}/resolve` endpoint — raises `ChaosEventResolved` event on the workflow instance via `DaprClient.RaiseWorkflowEventAsync`; returns HTTP 202

## 5. Pub/Sub Subscriptions

- [ ] 5.1 Add Dapr pub/sub subscription handler for `weather.alert` topic — raises `WeatherAlertReceived` (with severity) on the workflow instance for the affected ride
- [ ] 5.2 Add Dapr pub/sub subscription handler for `mascot.in-restricted-zone` topic — raises `MascotIntrusionReceived` on the workflow instance for the affected ride
- [ ] 5.3 Add Dapr pub/sub subscription handler for `ride.malfunction` topic — raises `MalfunctionReceived` on the workflow instance for the affected ride
