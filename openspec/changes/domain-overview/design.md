## Context

This is a greenfield project. No domain model exists yet. All 13 subsequent changes depend on an agreed vocabulary and set of invariants before implementation begins. Without this foundation, each service team risks building subtly incompatible interpretations of what a "Ride", "Session", or "Chaos Event" means, causing integration failures at the Dapr Workflow boundary.

The system simulates a theme park ride control system built as a .NET Aspire + Dapr Workflows conference demo. Seven microservices collaborate via Dapr service invocation, pub/sub, and workflow external events. Establishing the aggregate boundaries and ownership model upfront prevents duplicated state and avoids distributed consistency bugs.

## Goals / Non-Goals

**Goals:**
- Define all core entities and their fields with canonical types
- Establish aggregate root ownership so each service knows what it owns and does not own
- Document all domain invariants that implementations must enforce
- Fix the 5 ride seed records (GUIDs, names, zones, capacities) so every service boots with identical reference data
- Define the workflowId naming convention so tracing and idempotency keys are consistent

**Non-Goals:**
- Database schema or EF Core model design (belongs to individual service changes)
- API contract definition (belongs to individual service spec changes)
- Dapr component configuration (belongs to the infrastructure change)
- UI design or operator workflow UX

## Decisions

### Decision 1: `Ride` is the central aggregate

**Choice**: The `Ride` aggregate owns operational status, the active passenger list, and active chaos events for a running session.

**Rationale**: All runtime events (chaos, boarding, status transitions) are scoped to a single ride. Placing this state in the `Ride` aggregate gives a single source of truth. The Rides Service is the system of record; other services reference the rideId but do not duplicate ride state.

**Alternative considered**: A separate `RideSession` entity as its own aggregate root. Rejected because it creates a split-brain problem — the workflow knows session state but the Rides Service knows ride state, requiring a synchronisation protocol that adds complexity without benefit at this scale.

### Decision 2: Passenger immutability after boarding

**Choice**: The passenger list is frozen once `LoadPassengersActivity` completes. No mid-session additions or removals are permitted.

**Rationale**: Refund calculation at session end must be deterministic. If passengers could be added or removed during a session, a workflow replay (Dapr Workflow determinism requirement) could produce a different refund batch than the original execution, violating idempotency.

**Alternative considered**: Mutable passenger list with a snapshot taken at refund time. Rejected because snapshots add state management complexity and obscure the moment of truth.

### Decision 3: Ephemeral vs durable state split

**Choice**:
- **Durable** (Dapr state store): `Ride`, `MaintenanceRecord`, `RefundBatch`
- **Ephemeral** (in-memory, pub/sub driven): `WeatherCondition`, `MascotPosition`

**Rationale**: Weather and mascot state is environmental context that changes frequently and is always re-publishable from its source service. Storing it durably would require cache invalidation logic. Durable aggregates represent business facts that must survive restarts (e.g. a maintenance record, a refund).

### Decision 4: Zone is a value object, not an entity

**Choice**: Zone is a constrained string enum (`Zone-A`, `Zone-B`, `Zone-C`), not a managed entity with its own service.

**Rationale**: Zones are stable reference data used solely for weather and mascot alert correlation. There is no zone lifecycle (create/update/delete) in scope for this demo. A value object avoids a zone management service that would add infrastructure overhead for no observable benefit.

### Decision 5: WorkflowId naming convention

**Choice**: `ride-{rideId}-{yyyyMMddHHmmss}` — e.g. `ride-a1b2c3d4-0001-0000-0000-000000000001-20250115143022`

**Rationale**: Human-readable IDs make Dapr Dashboard traces and logs immediately interpretable during a conference demo. The timestamp suffix allows a ride to be restarted on the same day without ID collision. The rideId prefix enables prefix-scanning for "all sessions for this ride".

**Alternative considered**: UUID v4 workflowIds. Rejected because opaque IDs make live demos harder to follow and debug on stage.

## Risks / Trade-offs

**[Risk] Zone constraint not enforced at compile time** → Mitigation: Define a `Zone` strongly-typed value object (record or readonly struct) in `ThemePark.Shared` with a private constructor and factory method that throws on invalid input. Validated at deserialization boundary.

**[Risk] Seed GUIDs hardcoded in multiple services** → Mitigation: Centralise seed data in a `RideSeedData` static class in `ThemePark.Shared` so there is exactly one source of truth; services reference it rather than re-declaring GUIDs.

**[Risk] Invariants documented but not enforced** → Mitigation: Each invariant in `domain-invariants/spec.md` has a corresponding unit test task in `tasks.md`. CI must pass before merge.

**[Risk] Workflow replay produces different outcomes if domain logic changes** → Mitigation: Document the Dapr Workflow determinism constraint in `ThemePark.Shared` XML docs. Any change to activity logic must version the workflow.
