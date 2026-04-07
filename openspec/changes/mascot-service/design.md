## Context

The themepark-ride-controller demo requires a source of live "chaos" events to showcase the system's safety response. The mascot service introduces simulated mascot characters whose random wandering can trigger ride interruptions, giving conference audiences a vivid demonstration. This is a brand-new service with no prior implementation — all state is ephemeral and lives in memory.

## Goals / Non-Goals

**Goals:**

- Provide a self-contained microservice that tracks mascot positions and emits safety events
- Drive a realistic demo loop: timer moves mascots → mascot enters ride zone → ride pauses → operator clears → ride resumes
- Expose a minimal REST surface aligned with the Minimal API / Vertical Slice ADRs
- Support an operator-facing "clear" action with a corresponding domain event
- Allow conference speakers to trigger a mascot intrusion on demand via a feature-flagged endpoint

**Non-Goals:**

- Persistent mascot state across restarts (ephemeral in-memory only)
- Real geospatial coordinates or map rendering
- Authentication / authorisation on any endpoint
- Mascot audio/visual assets or UI

## Decisions

### 1. In-memory state store

**Decision**: Mascot positions are stored as an in-process dictionary, reset to `Park-Central` on every service start.

**Rationale**: The mascot positions are simulation state, not business data. Introducing a database or Dapr state store would add infrastructure complexity with no demo benefit. Ephemerality is acceptable and even desirable — the demo always starts from a clean slate.

**Alternatives considered**: Dapr state store (Redis) — rejected because persistence is unnecessary and complicates local setup.

---

### 2. IHostedService for the movement timer

**Decision**: A single `IHostedService` implementation runs the 45-second movement loop.

**Rationale**: `IHostedService` integrates cleanly with the .NET Generic Host lifetime, receives `CancellationToken` on shutdown, and requires no external scheduling infrastructure. The timer interval is read from configuration so it can be shortened for demos.

**Alternatives considered**: `System.Threading.Timer` ad-hoc — rejected because it bypasses host lifecycle and makes testing harder.

---

### 3. At-most-one mascot per ride zone

**Decision**: A given ride zone (Zone-A, Zone-B, Zone-C) can contain at most one mascot at any time. The movement algorithm skips a target zone if it is already occupied.

**Rationale**: Simplifies the demo narrative — one mascot per ride makes the "which mascot is blocking which ride" story clear. The ride controller only needs to track a single blocking mascot per ride.

**Alternatives considered**: Multiple mascots per zone — rejected as it complicates the clearing flow and muddies the demo story.

---

### 4. simulate-intrusion bypasses the timer

**Decision**: `POST /mascots/simulate-intrusion` immediately updates in-memory state and publishes `mascot.in-restricted-zone` without waiting for the next timer tick.

**Rationale**: Conference speakers need deterministic, immediate reactions during a live demo. Waiting up to 45 seconds is not viable on stage.

**Alternatives considered**: Shortening the timer interval dynamically — rejected because it affects the whole simulation loop rather than one targeted event.

---

### 5. Feature flag for simulate-intrusion

**Decision**: The `POST /mascots/simulate-intrusion` endpoint is only registered when `Dapr:DemoMode` is `true`.

**Rationale**: Prevents accidental activation in non-demo environments. The flag is already used elsewhere in the project for similar demo-only affordances.

## Risks / Trade-offs

- **Race condition on in-memory state** → Mitigation: Use a lock or `ConcurrentDictionary` around all state mutations; the timer and HTTP handler can fire concurrently.
- **Lost events on restart** → Accepted trade-off; ephemeral state is by design. Operators should clear mascots before stopping the service.
- **simulate-intrusion conflicts with timer** → Mitigation: If the timer fires shortly after a manual intrusion, it may move the mascot away before the operator clears. Document this as expected demo behaviour.
- **No retry on Dapr pub/sub publish failure** → Mitigation: Dapr sidecar handles at-least-once delivery; service only needs to fire-and-forget.
