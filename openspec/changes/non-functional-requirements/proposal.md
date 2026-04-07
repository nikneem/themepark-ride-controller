## Why

A conference demo app must be visually compelling — attendees need to see traces, metrics, and logs flowing live through the Aspire dashboard as chaos events fire. Without non-functional requirements (observability, health checks, resilience, and a security baseline) implemented consistently across all 7 microservices, the demo loses half its visual impact and lacks the production-grade patterns the talk aims to showcase.

## What Changes

- Implement `ThemePark.Aspire.ServiceDefaults` with an `AddServiceDefaults` extension method that centralises OTel traces, metrics, and logs; health check endpoints; and resilience defaults
- Wire `AddServiceDefaults` into all 7 API projects (ControlCenter, Rides, Queue, Maintenance, Weather, Mascots, Refunds)
- Add custom OTel metrics: `rides_started_total`, `rides_completed_total`, `rides_failed_total`, `chaos_events_total{type}`, `refunds_issued_total`, `active_workflows`
- Expose `GET /health/live` and `GET /health/ready` on every service
- Apply retry (3×, exponential backoff) and 30 s timeout policies on all Dapr service-invocation HttpClient calls
- Guard demo-mode simulation endpoints behind a `Dapr:DemoMode` feature flag (default: `false`)
- Enforce no hard-coded secrets — Aspire secrets or environment variables only

## Capabilities

### New Capabilities

- `observability`: OTel traces, metrics, and structured logs wired via ServiceDefaults; custom metric instruments; log scope fields (`rideId`, `workflowId`, `step`); trace propagation across Dapr service invocation and pub/sub
- `health-checks`: `/health/live` (liveness) and `/health/ready` (readiness — Dapr sidecar + state store reachable) on every API
- `resilience`: Retry policy (3 retries, exponential backoff 2 s/4 s/8 s) and 30 s timeout on all Dapr HttpClient calls via Microsoft.Extensions.Http.Resilience
- `security-baseline`: `Dapr:DemoMode` feature flag guarding simulation endpoints; prohibition on hard-coded secrets; inter-service mTLS via Dapr

### Modified Capabilities

<!-- No existing specs are changing requirements -->

## Impact

- **ThemePark.Aspire.ServiceDefaults** — new project; core library consumed by all services
- **ThemePark.Aspire.AppHost** — references ServiceDefaults; configures Aspire resource health probes
- **All 7 API projects** — call `AddServiceDefaults`; emit custom metrics; use resilience pipeline; expose health endpoints; respect DemoMode flag
- **Dependencies added**: `Microsoft.Extensions.Http.Resilience`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `AspNetCore.HealthChecks.Dapr` (or equivalent)
