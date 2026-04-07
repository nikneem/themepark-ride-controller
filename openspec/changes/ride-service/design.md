## Context

The Ride Service (`ThemePark.Rides.Api`) is responsible for tracking and mutating the physical operational state of each theme park ride. It is called exclusively by Control Center workflow activities via Dapr service invocation. Currently no implementation exists — this design establishes the foundational architecture for all ride state management.

Rides have a well-defined lifecycle: they start Idle, transition through Running, Paused, and back, and can fail or be placed in Maintenance. All state must survive process restarts and be consistent with what the workflow orchestrator expects.

## Goals / Non-Goals

**Goals:**
- Implement all 6 ride endpoints (get, start, pause, resume, stop, simulate-malfunction)
- Persist ride state durably in the Dapr state store (key: `ride-state-{rideId}`)
- Pre-seed 5 rides at startup via an `IHostedService`
- Enforce valid status transitions and return 409 Conflict when preconditions fail
- Publish `ride.malfunction` pub/sub events for the demo malfunction simulation
- Guard `simulate-malfunction` behind the `Dapr:DemoMode` feature flag

**Non-Goals:**
- Passenger boarding/disembarking logic (separate service concern)
- Ride scheduling or maintenance windows
- Persistent audit log of state transitions
- Authentication/authorisation on endpoints (handled at gateway level)

## Decisions

### Decision 1: Dapr state store for ride persistence (not a relational database)

**Chosen**: Dapr state store with key `ride-state-{rideId}`.

**Rationale**: This service is a demo/conference app with no complex relational queries. The Dapr state store is already available in the Aspire environment (backed by Redis in dev), has zero additional infrastructure cost, and aligns with the Dapr-first architectural stance. Ride state is a simple document keyed by ride ID.

**Alternative considered**: PostgreSQL via EF Core — rejected because it adds infrastructure complexity (migration management, connection pooling) with no benefit for this read/write-by-key pattern.

---

### Decision 2: Pre-seed rides at startup via `IHostedService`

**Chosen**: Register an `IHostedService` (`RideSeedService`) that writes the 5 default rides to the state store on startup if they don't already exist.

**Rationale**: Seeding at startup ensures the system is always in a known state after a fresh deploy or restart, without requiring a separate migration step. Using `IHostedService` integrates cleanly with the .NET host lifecycle and is observable via structured logs.

**Alternative considered**: Static seed data committed to Redis — rejected because it couples the data to infrastructure provisioning rather than the application itself.

---

### Decision 3: `RideStatus` enum replaces ad hoc string status

**Chosen**: `RideStatus` enum with values: `Idle`, `Running`, `Paused`, `Maintenance`, `Resuming`, `Completed`, `Failed`.

**Rationale**: A typed enum prevents misspellings, enables exhaustive switch expressions for transition validation, and provides a single source of truth for all status values across the domain and API layers.

**Alternative considered**: String constants — rejected due to fragility and lack of compile-time safety.

---

### Decision 4: Vertical slice structure, one folder per feature

**Chosen**: Feature folders under `ThemePark.Rides.Api`: `GetRide/`, `StartRide/`, `PauseRide/`, `ResumeRide/`, `StopRide/`, `SimulateMalfunction/`, `_Shared/`.

**Rationale**: Consistent with ADR-0007 (Vertical Slice Architecture). Each slice owns its request/response models, handler, and endpoint registration. Shared domain types (RideState, RideStatus) live in `_Shared/` or the `ThemePark.Rides` domain library.

---

### Decision 5: `simulate-malfunction` guarded by feature flag `Dapr:DemoMode`

**Chosen**: Read `Dapr:DemoMode` (bool, default `false`) from configuration. Return 404 if the flag is disabled.

**Rationale**: The malfunction simulation is a demo-only capability. Using a feature flag prevents accidental triggering in non-demo environments while keeping the endpoint in the codebase for reuse.

## Risks / Trade-offs

- **Dapr state store consistency** → Dapr provides last-write-wins semantics with optional ETags. Without optimistic concurrency, rapid concurrent status updates could cause lost writes. Mitigation: For a single-threaded demo workflow this is acceptable; ETag-based concurrency can be added later if needed.
- **Startup seed race condition** → If multiple instances start simultaneously, both may attempt to seed. Mitigation: The seed uses "upsert-if-not-exists" logic (read-then-write with ETag), acceptable for demo scale.
- **Feature flag is static config** → `Dapr:DemoMode` is read at startup, not dynamically toggled. Mitigation: Sufficient for conference demo; dynamic flag support (e.g., via Dapr configuration API) is a future enhancement.
