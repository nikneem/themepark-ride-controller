## Context

The themepark-ride-controller demo simulates a theme park ride control system using .NET Aspire and Dapr Workflows. The `LoadPassengersActivity` step within the Dapr workflow needs a source of passengers to fill each ride session. Without a Queue Service, that activity has nothing to work with, making the end-to-end demo non-functional.

The Queue Service is a new standalone microservice (`ThemePark.Queue.Api`) that owns the concept of waiting passengers per ride. It exposes a minimal HTTP surface consumed via Dapr service invocation.

## Goals / Non-Goals

**Goals:**
- Provide a read endpoint for current queue state per ride
- Provide an atomic dequeue endpoint that returns a boarding manifest
- Provide a demo seeding endpoint gated by a feature flag
- Persist queue state in the Dapr state store so it survives service restarts within a demo session

**Non-Goals:**
- Real-time push notifications when queue length changes
- Persistent passenger identity management (passengers exist only in the queue)
- Multi-tenant or multi-park support
- Authentication or authorization on any endpoint

## Decisions

### Decision 1: Dapr state store for queue persistence

**Choice:** Store each ride's queue as an ordered list serialized to JSON under the key `queue-{rideId}` in the Dapr state store.

**Rationale:** Dapr state store (Redis in dev) is already available in the Aspire orchestration. Adding a database solely for an ephemeral demo queue adds unnecessary infrastructure complexity. The list fits naturally as a single JSON array — reads and writes are always per-ride, so there is no need for querying across rides.

**Alternatives considered:**
- In-memory `ConcurrentQueue<T>`: Simpler but loses state on restart and cannot be shared across scaled instances.
- Dedicated Redis lists: Possible, but bypasses the Dapr abstraction layer and couples the service to Redis.

---

### Decision 2: Atomic load-dequeue in a single state transaction

**Choice:** The `POST /queue/{rideId}/load` handler reads the current queue, splices off up to `capacity` passengers, writes the remainder back, and returns the manifest — all within a single Dapr state operation using ETags for optimistic concurrency.

**Rationale:** The workflow may call `LoadPassengersActivity` concurrently for different rides, and the demo could be scaled to multiple instances. An atomic read-modify-write with ETag retry protects against double-dequeuing the same passenger.

**Alternatives considered:**
- Two separate calls (read then delete): Introduces a window for concurrent dequeue of the same passengers.
- Dapr distributed lock: Higher complexity; ETag retry is sufficient for low-concurrency demo use.

---

### Decision 3: Bogus for passenger name generation in simulate-queue

**Choice:** Use the `Bogus` library (already a test dependency) to generate realistic passenger names during queue seeding.

**Rationale:** Bogus is already available in the solution for test data. Reusing it in the demo-only endpoint keeps dependencies minimal and produces human-readable names that make the demo more engaging.

**Alternatives considered:**
- Hardcoded name list: Tedious to maintain, less varied.
- External name API: Network dependency in a demo environment is undesirable.

---

### Decision 4: Feature flag gates simulate-queue

**Choice:** The `POST /queue/{rideId}/simulate-queue` endpoint is only registered when `Dapr:DemoMode` is `true` in configuration.

**Rationale:** The simulate endpoint is a demo convenience, not production functionality. Gating it by feature flag ensures it cannot be called in any environment where `DemoMode` is not explicitly enabled, following the existing pattern used elsewhere in the solution.

---

### Decision 5: Estimated wait calculated in-process

**Choice:** `estimatedWaitMinutes = waitingCount / averageLoadCapacity * averageRideDurationMinutes`, with `averageLoadCapacity` and `averageRideDurationMinutes` read from `appsettings.json`.

**Rationale:** This is a display convenience for the demo UI. A simple formula configured via `appsettings.json` is sufficient. No ML or historical data is needed.

## Risks / Trade-offs

- **ETag retry loop under high concurrency** → In the demo context concurrency is low; a fixed retry cap (e.g., 5 attempts) prevents infinite loops.
- **Dapr state store size** → Very large seeded queues could hit state store limits; `simulate-queue` should document a reasonable upper bound (e.g., 10 000 passengers).
- **Bogus in production binary** → If `DemoMode` is false, the Bogus code path is never exercised, but the dependency is still compiled in. Acceptable for a demo project; could be isolated to a separate assembly if that becomes a concern.

## Migration Plan

1. Add `ThemePark.Queue.Api` project to the solution.
2. Register it in the Aspire AppHost alongside existing services.
3. Configure Dapr sidecar with app-id `queue-service` and state store binding.
4. Update `LoadPassengersActivity` to call `queue-service` via Dapr service invocation instead of any stub.
5. No data migration required — queues are ephemeral demo state.
