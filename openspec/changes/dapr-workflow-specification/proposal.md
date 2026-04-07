## Why

The themepark-ride-controller demo lacks a centrepiece that showcases Dapr Workflows in action. Without a long-running, event-driven workflow, the demo cannot illustrate how Dapr handles complex orchestration — fan-out pre-flight checks, external event reactions, timeouts, and compensation — all in a single coherent business process. The `RideWorkflow` fills that gap and becomes the live demonstration of Dapr's workflow engine.

## What Changes

- Introduce a new `RideWorkflow` Dapr Workflow class in `ThemePark.ControlCenter` that orchestrates the full lifecycle of a theme park ride from pre-flight to completion or failure.
- Add 10 Dapr Workflow activities that each invoke a downstream service via Dapr service invocation:
  - `CheckRideStatusActivity`
  - `CheckWeatherActivity`
  - `CheckMascotActivity`
  - `LoadPassengersActivity`
  - `StartRideActivity`
  - `PauseRideActivity`
  - `ResumeRideActivity`
  - `TriggerMaintenanceActivity`
  - `IssueRefundActivity`
  - `CompleteRideActivity`
- Add a POST `/api/rides/{rideId}/start` endpoint that schedules the workflow.
- Add POST endpoints for operator actions: `/api/rides/{id}/maintenance/approve` and `/api/rides/{id}/events/{eventId}/resolve`.
- Wire up three pub/sub subscriptions (`weather.alert`, `mascot.in-restricted-zone`, `ride.malfunction`) to raise external events into the running workflow.

## Capabilities

### New Capabilities

- `ride-workflow`: The main `RideWorkflow` class and all 10 activity classes — covers the happy path: pre-flight checks (status, weather, mascot), passenger loading, ride start, timed running loop, and ride completion.
- `workflow-external-events`: External event handling during the running loop — `WeatherAlertReceived`, `MascotIntrusionReceived`, `MalfunctionReceived`, `MaintenanceApproved`, and `ChaosEventResolved` — including timeout behaviour for each wait state.
- `workflow-compensation`: The failure path triggered by severe conditions or timeout expiry — pausing the ride, issuing passenger refunds, requesting maintenance, and recording a Failed outcome.

### Modified Capabilities

## Impact

- **ThemePark.ControlCenter**: New `Workflows/` folder containing `RideWorkflow.cs`, `RideWorkflowInput.cs`, and all 10 activity classes; updated Minimal API route registration; new pub/sub subscription handlers.
- **Dapr sidecar config**: Workflow component registration and pub/sub topic subscriptions (`weather.alert`, `mascot.in-restricted-zone`, `ride.malfunction`).
- **AppHost**: No structural changes required; existing Dapr resource wiring covers the new workflow.
- **Downstream services** (ride-service, weather-service, mascot-service, queue-service, maintenance-service, refund-service): Called via Dapr service invocation — no changes to those services for this change.
- **Dependencies**: `Dapr.Workflow` NuGet package must be added to `ThemePark.ControlCenter`.
