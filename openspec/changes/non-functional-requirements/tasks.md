## 1. ServiceDefaults Project

- [ ] 1.1 Create `ThemePark.Aspire.ServiceDefaults` class library project and add it to the solution
- [ ] 1.2 Add NuGet dependencies: `Microsoft.Extensions.Http.Resilience`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`
- [ ] 1.3 Implement `AddServiceDefaults(this IHostApplicationBuilder builder)` extension method as the single entry point wiring all NFR concerns

## 2. Observability

- [ ] 2.1 Register OTel traces in `AddServiceDefaults`: `AddAspNetCoreInstrumentation`, `AddHttpClientInstrumentation`, `AddSource("ThemePark.*")`, OTLP exporter
- [ ] 2.2 Register OTel metrics in `AddServiceDefaults`: `AddAspNetCoreInstrumentation`, `AddMeter("ThemePark.*")`, OTLP exporter
- [ ] 2.3 Register OTel logs in `AddServiceDefaults`: `AddOpenTelemetry().WithLogging(...)` with OTLP exporter; configure JSON structured logging formatter
- [ ] 2.4 Define custom `Meter` instruments per service (Rides, Queue, Maintenance, Weather, Mascots, Refunds, ControlCenter): `rides_started_total`, `rides_completed_total`, `rides_failed_total`, `chaos_events_total{type}`, `refunds_issued_total`, `active_workflows` gauge
- [ ] 2.5 Instrument workflow activities to emit custom metric increments and wrap log statements in `ILogger.BeginScope` with `rideId`, `workflowId`, and `step` fields

## 3. Health Checks

- [ ] 3.1 Register `/health/live` (liveness — no dependencies) and `/health/ready` (readiness — Dapr sidecar probe) in `AddServiceDefaults` via `IHealthChecksBuilder`
- [ ] 3.2 Implement `DaprSidecarHealthCheck` that GETs `http://localhost:3500/v1.0/healthz` and returns `Unhealthy` on non-200 or network failure
- [ ] 3.3 Map health check endpoints in the Minimal API pipeline (`app.MapHealthChecks("/health/live", ...)` and `app.MapHealthChecks("/health/ready", ...)`) — add helper `UseServiceDefaults(this WebApplication app)` if needed

## 4. Resilience

- [ ] 4.1 Configure `ConfigureHttpClientDefaults` in `AddServiceDefaults` with a custom resilience pipeline: 3 retries (exponential backoff 2 s / 4 s / 8 s) on transient HTTP errors, total timeout of 30 s

## 5. Security Baseline

- [ ] 5.1 Add `Dapr:DemoMode` configuration key (default `false`) to each service's `appsettings.json`; document in README that simulation endpoints require this flag
- [ ] 5.2 Add DemoMode guard to simulation endpoint handlers (`simulate-malfunction`, `weather/simulate`, `mascots/simulate-intrusion`) returning HTTP 403 when flag is `false`
- [ ] 5.3 Audit codebase for hard-coded secrets; replace any found values with environment variable or Aspire secrets references

## 6. Wire ServiceDefaults into All Services

- [ ] 6.1 Add `ThemePark.Aspire.ServiceDefaults` project reference to all 7 API projects
- [ ] 6.2 Call `builder.AddServiceDefaults()` in `Program.cs` of each service (ControlCenter, Rides, Queue, Maintenance, Weather, Mascots, Refunds)

## 7. Validation

- [ ] 7.1 Run the Aspire AppHost locally and confirm traces, metrics, and logs appear in the Aspire dashboard for at least one ride workflow execution
- [ ] 7.2 Verify `/health/live` returns 200 and `/health/ready` returns 200 on all services when the Aspire environment is running
- [ ] 7.3 Verify `/health/ready` returns 503 when the Dapr sidecar is stopped for one service
- [ ] 7.4 Confirm a simulation endpoint returns 403 when `Dapr:DemoMode` is not set, and 200 when set to `true`
