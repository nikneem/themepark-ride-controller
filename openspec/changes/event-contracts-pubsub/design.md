## Context

The theme park ride-control system is a distributed .NET 10 application composed of Weather Service, Mascot Service, Ride Service, Maintenance Service, and Control Center API. Services communicate asynchronously via Dapr pub/sub backed by Redis. Currently no formal contracts exist for the event payloads, so each service is free to define its own schema — causing silent deserialization failures and brittle subscriber code.

This design formalises all six pub/sub topics, their payload schemas (as C# records), the Dapr component configuration, and the subscription registration pattern for Control Center API.

## Goals / Non-Goals

**Goals:**
- Define a single source of truth for all pub/sub payload schemas as C# records
- Configure the `themepark-pubsub` Dapr component in Aspire AppHost with Redis backing
- Wire dead letter topics (`{topic}.deadletter`) for every event
- Register Dapr subscriber endpoints in Control Center API for all five inbound topics
- Enable `ride.status-changed` to be forwarded from Control Center to the frontend via SSE

**Non-Goals:**
- Message versioning / schema evolution strategy (deferred)
- Publishing from services other than defining the record types they must use
- Frontend SSE client implementation
- Authentication or authorisation on Dapr subscriptions

## Decisions

### Decision 1: C# records with System.Text.Json camelCase serialisation

**Chosen**: All event payloads are C# `record` types serialised with `JsonSerializerOptions` configured for camelCase property naming (`JsonNamingPolicy.CamelCase`) and `JsonStringEnumConverter`.

**Alternatives considered**:
- `class` types: Records are preferred for immutable, value-semantics DTOs. Records provide built-in equality and `with` expressions useful in tests.
- Newtonsoft.Json: The project standardises on `System.Text.Json` to align with ASP.NET Core 10 defaults and reduce dependencies.

### Decision 2: Shared class library for event contracts

**Chosen**: Event record types live in a shared `ThemePark.EventContracts` class library project referenced by all services that publish or subscribe.

**Alternatives considered**:
- Copy the records into each service: Creates drift and defeats the purpose of formal contracts.
- NuGet package: Appropriate for a production system; for a conference demo a project reference is simpler and avoids versioning overhead.

### Decision 3: `eventId` (UUID) mandatory on every event

**Chosen**: Every event payload includes `eventId` (a `Guid`) so that consumers can detect and discard duplicate deliveries.

**Rationale**: Dapr at-least-once delivery guarantees means duplicate messages are possible. The `eventId` gives downstream consumers a deduplication key without coupling them to Dapr message metadata.

### Decision 4: UTC ISO 8601 timestamps

**Chosen**: All timestamp fields use `DateTimeOffset` (serialised to ISO 8601 UTC, e.g. `2024-06-01T10:00:00Z`).

**Rationale**: Avoids timezone ambiguity in a demo that may run across multiple developer machines.

### Decision 5: Dead letter topics via Dapr component config

**Chosen**: Each subscription declares a `deadLetterTopic` named `{topic}.deadletter`. Failed messages are automatically routed there by Dapr after exhausting retries.

**Rationale**: Provides observability into failures without requiring custom error-handling logic in each subscriber. Dead letter topics can be monitored via the Aspire dashboard.

### Decision 6: Subscription registration via Dapr programmatic subscriptions

**Chosen**: Subscriptions are registered using `app.MapSubscribeHandler()` and `[Topic]` attribute on minimal API endpoints in Control Center API, consistent with ADR-0005 (Minimal APIs).

**Alternatives considered**:
- Declarative subscriptions (YAML): Less discoverable in code; doesn't compose well with dependency injection in minimal APIs.

## Risks / Trade-offs

- **Redis restart clears in-flight messages** → Acceptable for a conference demo; not a concern for production use.
- **No schema validation at the Dapr layer** → Malformed messages will cause deserialization exceptions routed to the dead letter topic. Dead letter monitoring provides visibility.
- **Shared project reference creates build coupling** → All services must rebuild when the contracts library changes. Acceptable given the demo scope; a NuGet package would decouple this in production.
- **SSE is unidirectional and stateless** → Clients that miss a `ride.status-changed` event will not receive a replay. A client reload fetches current ride state via the REST API.

## Migration Plan

1. Add `ThemePark.EventContracts` class library to the solution
2. Register Dapr pub/sub component in Aspire AppHost
3. Add event record types per capability spec
4. Wire subscriber endpoints in Control Center API
5. Update publisher services to use the shared record types
6. Smoke-test via `/weather/simulate`, `/mascots/simulate-intrusion`, `/rides/{id}/simulate-malfunction`, and maintenance endpoints
7. Verify dead letter topics appear in the Aspire / Dapr dashboard on a simulated failure

**Rollback**: Remove the `[Topic]` attributes and subscriber endpoints from Control Center API. Services can revert to their local payload types without system-wide impact.

## Open Questions

- Should the `ThemePark.EventContracts` library be a separate project in the solution root or placed inside `src/Shared/`? (Assumed: `src/Shared/ThemePark.EventContracts`)
- Is a retry policy needed on subscribers, or is the Dapr default (3 retries) sufficient for the demo?
