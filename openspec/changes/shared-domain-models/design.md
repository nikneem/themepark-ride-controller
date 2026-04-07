## Context

Each of the 7 microservices currently contains only a placeholder `Class1.cs`. No shared domain types exist yet — this is a greenfield implementation. Without a shared library, each service would independently define types like `RideStatus` or `Passenger`, creating inconsistency across service boundaries and breaking integration event contracts.

The system uses Dapr pub/sub for inter-service communication, meaning integration events must be structurally consistent across producers and consumers. A shared `IntegrationEvent` base record enforces this contract.

## Goals / Non-Goals

**Goals:**

- Establish a single `ThemePark.Shared` project as the authoritative source for all domain vocabulary types
- Ensure all enums, records, and the integration event base are immutable and dependency-free
- Provide deterministic ride seed data (stable GUIDs) so all services agree on ride identities during local development and testing
- Reference `ThemePark.Shared` from all 7 service projects

**Non-Goals:**

- Service-specific DTOs or request/response models (those remain in each service project)
- Persistence logic, serialization configuration, or any infrastructure concern in `ThemePark.Shared`
- Versioning strategy for shared types beyond the current release

## Decisions

### Decision 1: Separate `ThemePark.Shared` project, not inside ControlCenter

**Chosen**: Standalone class library `ThemePark.Shared`

**Rationale**: Placing shared types inside `ThemePark.ControlCenter` would force all 6 other services to take a transitive dependency on ControlCenter's infrastructure, commands, and handlers. A dedicated shared project has no service-level dependencies and can be referenced freely.

**Alternative considered**: NuGet package — rejected as premature for a demo app; a project reference is simpler and avoids a publish/consume cycle during development.

### Decision 2: `IntegrationEvent` as a base record with `EventId` + `OccurredAt`

**Chosen**: `public abstract record IntegrationEvent(string EventId, DateTimeOffset OccurredAt)`

**Rationale**: All Dapr pub/sub payloads inherit from this base, ensuring every event carries a stable identity (`EventId` as UUID string) and a timestamp. This enables idempotency checks and event ordering without service-specific boilerplate.

**Alternative considered**: Interface `IIntegrationEvent` — records cannot implement interface properties via positional syntax cleanly; a base record gives inheriting records positional constructor chaining for free.

### Decision 3: Ride seed data as `static readonly` fields in `RideCatalog`

**Chosen**: `public static class RideCatalog` with `static readonly RideInfo` fields and a `static readonly IReadOnlyList<RideInfo> All` collection

**Rationale**: Deterministic GUIDs are critical — if services independently generate ride IDs they will never agree. Hard-coded stable GUIDs in a shared constant class guarantee every service refers to the same ride identities in dev/test. A static class with named fields also enables compile-time references (e.g., `RideCatalog.ThunderMountain.RideId`).

**Alternative considered**: JSON seed file loaded at startup — adds I/O and ordering complexity; a pure static class is simpler and has no startup cost.

### Decision 4: All records are `sealed` and use positional syntax

**Chosen**: `public sealed record Passenger(string PassengerId, string Name, bool IsVip)`

**Rationale**: Sealing prevents accidental inheritance hierarchies in consuming services. Positional syntax provides built-in deconstruction, value equality, and `with`-expression support — the full immutability story with minimal boilerplate.

### Decision 5: No infrastructure dependencies in `ThemePark.Shared`

**Chosen**: `ThemePark.Shared` references only `Microsoft.NET.Sdk` (no NuGet packages)

**Rationale**: Adding JSON serialization attributes, EF Core annotations, or Dapr SDK types to `ThemePark.Shared` would force all consumers to pull those transitive dependencies. Serialization concerns belong in each service's infrastructure layer.

## Risks / Trade-offs

- **Risk: Services need types not in Shared** → Mitigation: Services define their own DTOs and map to/from shared types. Shared is the floor, not the ceiling.
- **Risk: Changing a shared enum value breaks all services** → Mitigation: Treat `ThemePark.Shared` as a versioned contract; additive changes only (new enum values, new optional record fields via `with`).
- **Risk: Stable GUIDs collide in production** → Mitigation: Seed data is for local dev/demo only; production ride catalog is loaded from a data store. The `RideCatalog` class is clearly documented as dev seed data.
