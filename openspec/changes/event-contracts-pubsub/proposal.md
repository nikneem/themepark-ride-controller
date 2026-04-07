## Why

The pub/sub event contracts are the integration layer between all services in the theme park ride-control system. Without formally defining them, each service implements its own payload shape and subscribers break silently — leading to data loss, silent failures, and difficult debugging during the conference demo.

## What Changes

- Define Dapr pub/sub component (`themepark-pubsub`) wired to Redis in Aspire AppHost
- Introduce C# record types for all six event topics with camelCase JSON serialisation
- Register Dapr subscribers in Control Center API for all inbound topics
- Configure dead letter topics (`{topic}.deadletter`) for failed message processing
- Document publisher/subscriber ownership for each topic

## Capabilities

### New Capabilities

- `pubsub-infrastructure`: Dapr pub/sub component configuration, serialisation rules, dead letter topic setup, and subscription registration pattern shared across services
- `chaos-event-contracts`: Payload schemas for `weather.alert`, `mascot.in-restricted-zone`, and `ride.malfunction` — the chaos/fault events that trigger ride workflow actions
- `maintenance-event-contracts`: Payload schemas for `maintenance.requested` and `maintenance.completed` — the maintenance lifecycle events
- `ride-status-events`: Payload schema for `ride.status-changed` and its SSE forwarding behaviour from Control Center to the frontend

### Modified Capabilities

<!-- No existing capabilities are changing -->

## Impact

- **Aspire AppHost**: New Dapr pub/sub component resource pointing at Redis
- **Control Center API**: Subscriber endpoints for all five inbound topics; SSE publisher for `ride.status-changed`
- **Weather Service / Mascot Service / Ride Service / Maintenance Service**: Must publish events using the defined record types and topic names
- **All services**: Shared event contract NuGet package (or shared project) providing the record types
- **Observability**: Dead letter topics feed into OpenTelemetry traces for failed message diagnostics
