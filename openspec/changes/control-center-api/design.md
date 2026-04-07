## Context

The Control Center API is a new .NET 10 Minimal API project that acts as the single gateway for operators and the frontend UI. It must orchestrate long-running ride sessions (spanning pre-flight checks, the ride itself, and post-ride cleanup), react to chaotic external events in real time, and push live status updates to connected clients. All inter-service communication uses Dapr service invocation; event-driven reactions use Dapr pub/sub.

## Goals / Non-Goals

**Goals:**
- Expose all 7 operator/frontend endpoints as Minimal API endpoints following ADR-0005 and ADR-0007 (vertical slice / feature folders)
- Implement `RideWorkflow` as a Dapr Workflow orchestrating the full ride session lifecycle with timeouts and compensation
- Subscribe to 4 Dapr pub/sub topics and inject external events into active workflows
- Stream real-time ride and chaos events to the frontend via SSE using `Channel<T>` per connection
- Follow CQRS handler pattern (ADR-0004) for all endpoint logic

**Non-Goals:**
- Persistence layer (ride definitions come from RideService via Dapr invocation; no local DB)
- Authentication / authorization (out of scope for demo)
- Horizontal SSE scaling (single-node in-memory channels are sufficient for demo)

## Decisions

### 1. Dapr Workflow for ride session lifecycle
**Decision**: Model each ride session as a `WorkflowActivity`-backed Dapr Workflow (`RideWorkflow`) keyed on `rideId`.  
**Rationale**: Dapr Workflow provides durable execution, replay-safe state, and built-in support for external event signals — exactly what the chaos event injection requires. Alternatives (plain Task orchestration, Sagas via pub/sub) lack durable replay and external event support without custom infrastructure.

### 2. Vertical slice / feature folder structure
**Decision**: One folder per feature under `ThemePark.ControlCenter/` (e.g. `GetAllRides/`, `StartRide/`), each containing its command/query, handler, and endpoint registration.  
**Rationale**: ADR-0007 mandates vertical slice; this keeps related code co-located and avoids cross-cutting coupling. The `Workflows/` and `EventSubscriptions/` folders follow the same pattern for their slices.

### 3. Channel<T> for SSE per connection
**Decision**: Each SSE client gets a dedicated `Channel<SseEvent>` registered in a `SseConnectionManager` singleton; pub/sub subscribers write to all open channels.  
**Rationale**: Avoids a full message broker for the demo. Channels provide backpressure and async enumeration natively. Risk of memory leak is mitigated by cancellation token on client disconnect.  
**Alternative considered**: SignalR — adds complexity and WebSocket negotiation overhead not needed for a one-way push stream.

### 4. Dapr service invocation for downstream calls
**Decision**: All calls to RideService, WeatherService, MascotService, and MaintenanceService go through `DaprClient.InvokeMethodAsync`, not direct HTTP.  
**Rationale**: Consistent with the Dapr-first architecture; enables mTLS, retries, and observability without code changes. Avoids hardcoded service URLs.

### 5. Pub/sub subscriber raises external event by workflow instance ID
**Decision**: The workflow instance ID is set to `ride-session-{rideId}`. Subscribers retrieve the active instance ID from a Dapr state store key `active-workflow-{rideId}` and call `DaprClient.RaiseWorkflowEventAsync`.  
**Rationale**: Workflow instance IDs must be known at raise time. Storing the ID in state store on workflow start decouples subscriber logic from the workflow startup path.

## Risks / Trade-offs

- **SSE client leak** → Mitigation: Register `CancellationToken` on `HttpContext.RequestAborted`; `SseConnectionManager.Remove` in a `finally` block.
- **Workflow replay idempotency** → Mitigation: All workflow activities must be side-effect-free on replay; use Dapr Workflow determinism rules (no `DateTime.Now`, no random, log only via workflow logger).
- **Fan-out pre-flight timeout** → Mitigation: Wrap parallel activity fan-out in `Task.WhenAll` with a `WaitForExternalEvent` timeout; if any check fails, run compensation activities and terminate workflow.
- **Duplicate pub/sub delivery** → Mitigation: Activities and external event handlers are idempotent by design; duplicate events are ignored if workflow is not in the expected state.

## Migration Plan

This is a greenfield project — no migration required. Steps to deploy:
1. Add project to AppHost `.csproj` and register Dapr sidecar in `AppHost/Program.cs`
2. Register Dapr components (state store, pub/sub) in `components/` directory
3. Deploy via `aspire publish` / container image

## Open Questions

- Should the SSE stream require a ride filter parameter, or always broadcast all rides? (Current spec: all rides, no filter)
- What is the exact JSON shape returned by downstream RideService for `GET /api/rides`? (To be aligned during RideService implementation)
