## Context

The Weather Service is a new autonomous microservice in the ThemePark demo application. It simulates dynamic weather conditions that affect ride operations. No weather service exists today. The service must emit pub/sub events that downstream ride-controller services will react to, and must be self-driving so that live demos progress naturally without operator intervention.

## Goals / Non-Goals

**Goals:**
- Run an autonomous timer loop that generates random weather and publishes Dapr pub/sub events
- Expose a query endpoint for current weather state
- Expose a feature-flag-guarded endpoint that forces a condition immediately (demo use)
- Keep implementation simple and stateless enough for a conference demo

**Non-Goals:**
- Persistent weather history or audit log
- Real weather data integration
- Per-ride granularity (zone-level is sufficient)
- Horizontal scaling / distributed state synchronisation

## Decisions

### Decision 1: IHostedService for the timer loop

**Choice:** `BackgroundService` (abstract base of `IHostedService`) for the periodic weather simulation.

**Rationale:** This is the idiomatic .NET way to run background work inside an ASP.NET Core host. It starts and stops with the application lifetime, requires no external scheduler, and integrates cleanly with DI.

**Alternative considered:** `System.Threading.Timer` registered as a singleton — rejected because lifecycle management is manual and error-prone.

### Decision 2: In-memory state for current weather

**Choice:** Store the latest generated condition in a private field on the simulation engine (registered as singleton).

**Rationale:** Weather state is ephemeral simulation data. It does not need to survive restarts; on restart the service begins in Calm state, which is safe. Using Dapr state store would add latency and infrastructure complexity with no benefit for this use case.

**Alternative considered:** Dapr state store — rejected because the data is intentionally transient and the overhead is unjustified.

### Decision 3: Probability weights in appsettings

**Choice:** Read `Weather:CalmWeight`, `Weather:MildWeight`, `Weather:SevereWeight` from configuration (defaulting to 60/30/10).

**Rationale:** Allows demo facilitators to increase Severe probability for effect without rebuilding. Keeps constants out of compiled code.

**Alternative considered:** Hard-coded constants — rejected because demo flexibility matters.

### Decision 4: Static zone-to-ride mapping in configuration

**Choice:** Zone membership is defined in `appsettings.json` under `Weather:Zones` and is not queryable at runtime.

**Rationale:** Zone assignments are stable for the demo. Making them dynamic would require a registry service, adding unnecessary complexity.

### Decision 5: Feature flag via `Dapr:DemoMode`

**Choice:** `POST /weather/simulate` is only registered (or returns 404) when `Dapr:DemoMode` is `true`.

**Rationale:** Prevents accidental use of the demo override in production-like environments while keeping the endpoint discoverable during live demos.

## Risks / Trade-offs

- **In-memory state lost on restart** → Acceptable for demo; service re-enters Calm state which is operationally safe.
- **No deduplication on pub/sub** → Downstream consumers should be idempotent; this is a demo constraint, not a production issue.
- **Timer drift under load** → `PeriodicTimer` in .NET 6+ handles drift gracefully; interval is configurable if needed.
- **DemoMode flag is process-level** → Anyone with access to the environment can toggle it; acceptable for a conference demo.
