## Context

The Theme Park Ride Controller models each ride as a long-running Dapr Workflow. Currently, ride "status" is an informal concept set ad hoc inside individual workflow activities via calls to the Ride Service. There is no central enforcement of valid state transitions, making it possible for a ride to appear in an inconsistent state (e.g., `Running` when no workflow is active) and making it difficult to reason about system state in the frontend or in tests.

This design formalises the ride lifecycle as a domain-level state machine living in `ThemePark.Rides` domain library, integrated into the Dapr Workflow in `ThemePark.ControlCenter`, persisted in the Dapr state store, and reflected to the frontend via pub/sub and SSE.

**Constraints (from Hexmaster ADRs):**
- ADR-0004: All business logic in CQRS handlers, thin endpoints.
- ADR-0007: Vertical Slice Architecture — state machine transitions are individual feature slices.
- ADR-0005: Minimal API endpoints — no MVC controllers.
- ADR-0003: .NET Aspire orchestration with Dapr sidecars.

## Goals / Non-Goals

**Goals:**
- A `RideStateMachine` class that enforces valid transitions and raises `RideStatusChanged` domain events.
- All workflow activities call `RideStateMachine.Transition(newState)` rather than setting status directly.
- Ride state is persisted in the Dapr state store (key: `ride-state-{rideId}`) and readable via the Ride Service API.
- `ride.status-changed` pub/sub messages are published by Control Center on every transition.
- The SSE endpoint (`GET /api/events/stream`) pushes `ride-status-changed` events to connected frontend clients.

**Non-Goals:**
- Distributed locking / optimistic concurrency between multiple workflow instances for the same ride (enforced by design: one active workflow per ride).
- Persistent event sourcing / full audit log of every transition (history endpoint shows session summaries, not individual transitions).
- Frontend state machine implementation (Angular handles display state reactively from SSE).

## Decisions

### Decision 1: State machine lives in `ThemePark.Rides` domain library

**Choice**: `RideStateMachine` is a plain domain class in `ThemePark.Rides` (the shared domain library), not in the workflow project or the API project.

**Rationale**: The state machine expresses domain rules — it has no infrastructure dependencies and should be testable without Dapr, ASP.NET, or any framework.

**Alternative considered**: Put the state machine in `ThemePark.ControlCenter` alongside the workflow. Rejected because it couples domain logic to infrastructure concerns and prevents the Ride Service from also enforcing transitions when called directly.

---

### Decision 2: State is persisted via Dapr state store in Ride Service

**Choice**: The Ride Service maintains authoritative ride state in the Dapr state store (key: `ride-state-{rideId}`). Every transition is persisted before the activity returns.

**Rationale**: Dapr state store provides durable, sidecar-managed persistence without requiring a database per service. In local development (Aspire), this is backed by Redis; in production it would be backed by Azure Cosmos DB or similar.

**Alternative considered**: Keep state in memory only inside the workflow. Rejected because workflow state is not queryable from outside the workflow instance, making the `GET /api/rides` endpoint dependent on the workflow being alive.

---

### Decision 3: `RideStatusChanged` is a domain event, not a Dapr pub/sub event

**Choice**: `RideStateMachine.Transition()` raises a `RideStatusChanged` C# domain event (plain record). The Ride Service's command handler then translates this to a Dapr pub/sub message on the `ride.status-changed` topic.

**Rationale**: Separating domain events from integration events keeps the domain model free of Dapr SDK references. The handler is the anti-corruption boundary.

**Alternative considered**: Have `RideStateMachine` directly publish to Dapr pub/sub. Rejected because it introduces infrastructure coupling in the domain layer.

---

### Decision 4: Transition validation uses an allowed-transitions lookup table

**Choice**: Valid transitions are declared as a static `Dictionary<RideStatus, IReadOnlySet<RideStatus>>`. An attempt to transition to an unlisted target state throws `InvalidRideTransitionException`.

**Rationale**: A lookup table is explicit, readable, and trivially testable. Every valid transition is visible in one place.

**Alternative considered**: A switch expression or state pattern. Rejected as more verbose for this use case; the table is more self-documenting.

---

### Decision 5: SSE is the real-time channel to the frontend

**Choice**: The Control Center API subscribes to `ride.status-changed` pub/sub events and forwards them to connected SSE clients via a `Channel<RideStatusChangedEvent>` per connection.

**Rationale**: SSE is simpler than SignalR for one-way server-to-client push, has no extra dependencies, and is well-supported in Angular's `EventSource` API. Matches the existing spec in `docs/frontend.md`.

**Alternative considered**: SignalR. Rejected for initial implementation to reduce complexity; can be swapped in later without changing the event contract.

## Risks / Trade-offs

| Risk | Mitigation |
|---|---|
| Workflow activity and Ride Service both write state — potential race condition if two activities run concurrently | Dapr Workflow serialises activities within a workflow instance; concurrent workflows for the same ride are prevented at the start endpoint (409 guard) |
| SSE channel memory leak if clients disconnect without cleanup | Use `CancellationToken` from the HTTP request; dispose the channel subscription on disconnect |
| State store becomes stale if a workflow crashes mid-transition | Implement a `ride.workflow-heartbeat` that the workflow sends every N seconds; Control Center detects missing heartbeats and marks the ride `Failed` (future work) |
| Breaking change to `RideOperationalStatus` enum in Ride Service | Rename in a single PR with find-and-replace; no external consumers in current codebase |

## Migration Plan

1. Add `RideStatus` enum and `RideStateMachine` to `ThemePark.Rides` domain library.
2. Update Ride Service state store read/write to use `RideStatus`.
3. Update all workflow activities in `ThemePark.ControlCenter` to call the state machine.
4. Add pub/sub publisher in Control Center for `ride.status-changed`.
5. Wire SSE endpoint to the status-changed channel.
6. Update `GET /api/rides` and `GET /api/rides/{id}/status` response DTOs.
7. Run integration tests end-to-end.

Rollback: The change is fully additive (except the enum rename). If the state machine is not registered, the Ride Service falls back to the previous behaviour (workflow activities set status directly). Feature flag `RideStateMachine:Enabled` gates the new path during rollout.

## Open Questions

- Should `Failed` → `Idle` transition happen automatically after compensation completes, or require an explicit operator reset action in the UI?
- What is the maximum number of SSE clients expected per deployment? (Affects channel buffer sizing.)
- Should the Dapr state store key for ride state include a version/ETag for optimistic concurrency?
