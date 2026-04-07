## Context

The themepark-ride-controller is a conference demo app built on .NET 10, C#, .NET Aspire, and Dapr Workflows. It comprises 7 microservices. Currently no shared non-functional foundation exists — each service would otherwise independently (and inconsistently) handle observability, health checks, resilience, and security. For a live demo, a consistent, visually rich observability story is essential.

## Goals / Non-Goals

**Goals:**
- Centralise all NFR wiring in `ThemePark.Aspire.ServiceDefaults` so a single `AddServiceDefaults()` call per service is sufficient
- Expose meaningful OTel traces, metrics, and structured logs through the Aspire dashboard during the demo
- Provide `/health/live` and `/health/ready` endpoints on every API so Aspire and operators can verify service health
- Apply resilience (retry + timeout) uniformly on all Dapr service-invocation calls
- Guard demo-mode simulation endpoints with a `Dapr:DemoMode` configuration flag

**Non-Goals:**
- Production-grade OTel sampling strategies (100% sampling is acceptable for a demo)
- Circuit breaker patterns (not required for initial implementation)
- Authentication / authorisation beyond inter-service mTLS provided by Dapr
- Centralised secret management beyond "no hard-coded secrets" enforcement

## Decisions

### Decision 1: ServiceDefaults extension centralises all NFR wiring

**Choice**: Single `AddServiceDefaults(this IHostApplicationBuilder builder)` extension method in `ThemePark.Aspire.ServiceDefaults` that registers OTel, health checks, and resilience defaults.

**Rationale**: Duplicating NFR boilerplate in 7 services creates drift and missed updates. A single call keeps services thin and the demo consistent.

**Alternatives considered**: Per-service manual registration — rejected because it produces 7 divergent configurations.

---

### Decision 2: Custom metrics via `System.Diagnostics.Metrics.Meter`

**Choice**: Define a `Meter` named `"ThemePark.ControlCenter"` (and per-service equivalents where needed). Register the meter name in `ServiceDefaults` via `AddMeter("ThemePark.*")` so the OTel pipeline captures all theme-park meters automatically.

**Rationale**: `System.Diagnostics.Metrics` is the .NET-idiomatic, OTel-compatible approach. Wildcard meter registration avoids manual per-meter wiring in ServiceDefaults.

**Alternatives considered**: Prometheus-only counters via `prometheus-net` — rejected; OTel-native approach is more portable and already integrated with the Aspire dashboard.

---

### Decision 3: Resilience via `Microsoft.Extensions.Http.Resilience` on Dapr HttpClient

**Choice**: Configure a `StandardResilienceHandler` (or custom pipeline) with 3 retries (exponential backoff: 2 s / 4 s / 8 s) and a 30 s total timeout on the `HttpClient` used for Dapr service invocation. Registered in `ServiceDefaults` via `AddStandardResilienceHandler` / `ConfigureHttpClientDefaults`.

**Rationale**: `Microsoft.Extensions.Http.Resilience` (Polly v8 under the hood) integrates cleanly with `IHttpClientFactory` and is the recommended approach in .NET Aspire. Centralising it in ServiceDefaults means workflow activities automatically inherit the policy.

**Alternatives considered**: Manual Polly `AsyncRetryPolicy` per activity — rejected; verbose and not composable.

---

### Decision 4: DemoMode as `IConfiguration["Dapr:DemoMode"]` bool checked in endpoint handler

**Choice**: Read `builder.Configuration["Dapr:DemoMode"]` (default `false`) inside each simulation endpoint handler, returning `403 Forbidden` when the flag is off.

**Rationale**: Keeping the check in the handler (not middleware) makes the intent explicit at the route level and avoids introducing a middleware dependency that would affect non-simulation routes.

**Alternatives considered**: Feature management middleware (`Microsoft.FeatureManagement`) — considered but rejected to avoid adding another dependency for a single boolean flag in a demo context.

---

### Decision 5: Readiness probe checks Dapr sidecar health endpoint

**Choice**: The `/health/ready` health check makes an HTTP GET to `http://localhost:3500/v1.0/healthz`. If the sidecar returns non-200, the check fails and the endpoint returns 503.

**Rationale**: Dapr sidecar availability is the primary external dependency for all Dapr-enabled services. Probing the well-known sidecar health URL is lightweight and requires no additional infrastructure.

**Alternatives considered**: State store ping via Dapr state API — more thorough but adds latency and complexity; deferred to a future enhancement.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| 100% OTel sampling rate generates high telemetry volume in long-running demos | Acceptable for a single-session conference demo; document the sampling config location for production guidance |
| Dapr sidecar health endpoint URL (`localhost:3500`) is hard-coded | Standard Dapr port; override via `DAPR_HTTP_PORT` environment variable if needed |
| `AddServiceDefaults` becomes a large method over time | Keep it focused; break into `AddObservability`, `AddHealthChecks`, `AddResilience` private helpers |
| Wildcard meter registration (`ThemePark.*`) may capture unintended meters | Acceptable in demo context; scope can be tightened by listing explicit meter names |
