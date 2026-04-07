## Why

The Control Center API is the operational backbone of the themepark ride controller demo — without it, operators have no way to start rides, react to chaos events, or observe system state. It must be built first as it drives all downstream service interactions and establishes the Dapr Workflow orchestration model for the entire system.

## What Changes

- Implement 7 REST endpoints for ride management, session control, and operator actions
- Implement `RideWorkflow` as a Dapr Workflow orchestrating a full ride session lifecycle (pre-flight → riding → post-ride)
- Implement 4 pub/sub event subscriptions (weather, mascot, malfunction, maintenance) that inject external events into active workflows
- Implement SSE stream endpoint pushing real-time ride and chaos event updates to the frontend

## Capabilities

### New Capabilities

- `ride-management-api`: All 7 HTTP endpoints — list rides, get status, start ride, approve maintenance, resolve chaos event, get history, and SSE stream
- `workflow-orchestration`: RideWorkflow Dapr Workflow class with 10 activities, external event handling, and timeout/compensation logic
- `chaos-event-handling`: Pub/sub subscriptions for weather.alert, mascot.in-restricted-zone, ride.malfunction, and maintenance.completed — raises external events into the active workflow
- `sse-stream`: Server-Sent Events endpoint delivering ride-status-changed, chaos-event-received, chaos-event-resolved, and ride-completed events to connected frontend clients

### Modified Capabilities

## Impact

- **ThemePark.ControlCenter.Api**: New project with Minimal API endpoints, Dapr Workflow registration, and pub/sub subscriptions
- **ThemePark.ControlCenter** (domain library): New domain models (Ride, RideSession, ChaosEvent), CQRS command/query handlers
- **Downstream services**: Called via Dapr service invocation — RideService, WeatherService, MascotService, MaintenanceService
- **Dapr components**: Redis pub/sub (dev), Redis state store
- **AppHost (.NET Aspire)**: Register new API project and Dapr sidecar
