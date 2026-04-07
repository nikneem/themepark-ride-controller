## Why

Weather is the most frequent chaos event in a theme park demo. An autonomous timer means the demo "runs itself" — operators see weather alerts fire naturally during a presentation without manual intervention, demonstrating real-time pub/sub and service-to-service reactions without scripted triggering.

## What Changes

- Introduce an internal `IHostedService` timer that runs every 60 seconds and randomly generates a weather condition
- Publish `weather.alert` Dapr pub/sub events for Mild and Severe conditions
- Expose `GET /weather/current` to query the current simulated condition
- Expose `POST /weather/simulate` (demo-mode only) to force a specific condition immediately

## Capabilities

### New Capabilities

- `weather-simulation-engine`: Internal timer loop that randomly generates weather conditions with configurable probability weights and publishes domain events via Dapr pub/sub
- `weather-query`: Endpoint that returns the current weather condition, severity, affected zones, and timestamp
- `weather-manual-trigger`: Feature-flag-guarded endpoint that forces a specific weather condition immediately for demo purposes

### Modified Capabilities

## Impact

- New service: `ThemePark.Weather.Api` (port 5104, Dapr app-id `weather-service`)
- Dapr pub/sub topic `weather.alert` introduced — downstream ride-controller services must subscribe to react
- AppHost registration required in the .NET Aspire AppHost project
- No breaking changes to existing services
