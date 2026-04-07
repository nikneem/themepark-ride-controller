## Context

`ThemePark.ControlCenter` is a .NET 10 / Dapr-enabled service that currently handles ride management via Minimal API endpoints and CQRS handlers. There is no long-running orchestration layer: each API call is fire-and-forget. The demo needs a centrepiece that shows Dapr Workflows orchestrating a multi-step business process with fan-out, external events, timeouts, and compensation.

The `Dapr.Workflow` SDK sits on top of the Dapr sidecar workflow engine (backed by Durable Task Framework). Workflows are deterministic functions that execute activities, fan out with `Task.WhenAll`, and suspend for external events via `WaitForExternalEventAsync`.

## Goals / Non-Goals

**Goals:**
- Implement a single `RideWorkflow` class in `ThemePark.ControlCenter` that encapsulates the full ride lifecycle.
- Implement all 10 activities, each calling a downstream service via Dapr service invocation.
- Support external event injection from pub/sub subscriptions and operator API endpoints.
- Handle all timeout scenarios (mascot cleared, weather cleared, maintenance approved, ride completed).
- Implement a compensation path (refund + maintenance) for any failure scenario.
- Retry each activity call up to 3 times with exponential backoff.

**Non-Goals:**
- Sub-workflow decomposition — the full lifecycle lives in one workflow class for demo clarity.
- Persistent state beyond the Dapr workflow state store — no separate database writes from within activities.
- Changes to any downstream service (ride-service, weather-service, etc.).
- Production-grade secrets management or multi-tenant isolation.

## Decisions

### Decision 1: Single `RideWorkflow` class, no sub-workflows

**Choice**: One workflow class with a linear orchestration method and a running loop.

**Rationale**: Sub-workflows add indirection without demo value. A single class makes the entire ride lifecycle readable in one file, which is ideal for a conference demo. Dapr supports sub-workflows but the added complexity is not justified here.

**Alternative considered**: Split into `PreFlightWorkflow`, `RunningWorkflow`, `CompensationWorkflow`. Rejected because it fragments the narrative and increases boilerplate.

---

### Decision 2: Fan-out via `Task.WhenAll` wrapping activity tasks

**Choice**: `CheckWeatherActivity` and `CheckMascotActivity` are scheduled with `context.CallActivityAsync` and awaited together with `Task.WhenAll`.

**Rationale**: This is the idiomatic Dapr Workflow fan-out pattern. It produces a single await point and allows both activities to execute concurrently on the Dapr scheduler. The workflow blocks until both complete or either throws.

**Alternative considered**: Sequential execution of the two checks. Rejected because parallel execution is faster and demonstrates fan-out — a key Dapr Workflow capability.

---

### Decision 3: Running loop with `WaitForExternalEventAsync` and `CancellationToken` timeout

**Choice**: The running loop is a `while(true)` that calls `Task.WhenAny` on:
- A `CreateTimer` task (90-second ride completion timer)
- One `WaitForExternalEventAsync` per expected external event

The first task to complete determines the branch.

**Rationale**: `WaitForExternalEventAsync` accepts a `CancellationToken` derived from `context.CreateTimer`, enabling clean timeout without busy-polling. Each loop iteration re-registers all listeners, so the workflow handles multiple events in sequence.

**Alternative considered**: A finite state machine with explicit state enum. Rejected because the `Task.WhenAny` pattern is more concise and idiomatic to Dapr Workflow's async model.

---

### Decision 4: Workflow ID format `ride-{rideId}-{timestamp}`

**Choice**: The workflow instance ID is `ride-{rideId}-{yyyyMMddHHmmss}` constructed at schedule time.

**Rationale**: Including `rideId` makes the workflow instance discoverable by ride without an external index. The timestamp suffix prevents ID collisions when the same ride is started multiple times (e.g., after maintenance). The format is human-readable in the Dapr dashboard.

**Alternative considered**: A random UUID. Rejected because it makes operator debugging harder — you cannot infer which ride a workflow instance belongs to without looking up state.

---

### Decision 5: Global retry policy per activity — 3 retries, exponential backoff

**Choice**: A shared `WorkflowTaskOptions` with `RetryPolicy` (maxAttempts=3, firstRetryInterval=2s, backoffCoefficient=2.0, maxRetryInterval=8s) is applied to every `CallActivityAsync` call.

**Rationale**: All activities call HTTP endpoints via Dapr service invocation. Transient network errors and sidecar restarts are expected in a demo environment. Centralising the policy avoids per-activity boilerplate and ensures consistent behaviour.

**Activity timeout**: 30 seconds per activity call (set via `WorkflowTaskOptions.SendTimeout`).

**Alternative considered**: Per-activity retry policies. Rejected because all activities have the same risk profile (HTTP calls over Dapr) and uniformity aids demo clarity.

---

### Decision 6: Pub/sub subscriptions raise external events, not direct API calls

**Choice**: The three pub/sub handlers (`weather.alert`, `mascot.in-restricted-zone`, `ride.malfunction`) call `DaprClient.RaiseWorkflowEventAsync` to inject events into the running workflow. Operator endpoints (`/maintenance/approve`, `/events/{id}/resolve`) do the same.

**Rationale**: This decouples the event sources from the workflow. The workflow only cares about named events. Any source can raise them. This is the intended Dapr Workflow external event pattern.

**Alternative considered**: Polling the Dapr state store from within the workflow. Rejected because it defeats the event-driven model and adds unnecessary complexity.

## Risks / Trade-offs

- **Workflow replay determinism** → Any non-deterministic code (DateTime.Now, random, I/O) inside the workflow orchestrator method will cause replay errors. Mitigation: all side-effects are in activities; orchestrator uses `context.CurrentUtcDateTime` for timestamps.
- **Long-running state size** → If many events are received, the workflow history grows. For a demo this is acceptable; in production, sub-workflows or continue-as-new would be used. Mitigation: document the limitation; not mitigated in this change.
- **Single running loop per ride** → If two `start` calls are made concurrently for the same ride, two workflow instances will start. Mitigation: `CheckRideStatusActivity` verifies the ride is Idle and will fail the second workflow immediately.
- **Dapr sidecar availability** → If the sidecar restarts mid-workflow, Dapr replays the history. The determinism requirement (see above) must be strictly observed. Mitigation: unit tests for activities, integration/manual tests for the full workflow path.

## Open Questions

- Should `WeatherAlertReceived` with severity=Mild auto-resolve after `ChaosEventResolved` is raised, or should `ResumeRideActivity` be called unconditionally after the wait? *(Assumed: `ChaosEventResolved` acts as the resume signal; workflow calls `ResumeRideActivity` upon receiving it.)*
- Is the 90-second `RideCompletedTimer` configurable per-ride or per-environment? *(Assumed: configured via `IConfiguration` at workflow schedule time and passed in `RideWorkflowInput`.)*
