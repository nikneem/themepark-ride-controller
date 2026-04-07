## Context

The ThemePark Ride Controller is a conference demo application built on .NET 10, Dapr Workflows, and .NET Aspire. It comprises 7 microservices, each with a scaffolded `.Tests` project. Currently, no standardised testing strategy exists: test naming is inconsistent, workflow scenarios are untested end-to-end, and there is no automated gate that validates the exact steps of the live demo before a presentation.

The primary risk is an on-stage failure during a conference demo caused by an untested state-transition or an uncaught regression. The testing strategy must be straightforward enough for every contributor to follow without friction.

## Goals / Non-Goals

**Goals:**
- Define a consistent unit-test style (naming, coverage targets, mocking, fake data) across all 7 services
- Introduce an integration-test project that exercises the full Aspire AppHost against a real Dapr sidecar and Redis, covering 6 critical ride-workflow scenarios
- Provide an automated acceptance test that mirrors the 7-step conference demo script
- Allow timeout-dependent workflow tests to run in seconds rather than minutes via time-provider injection
- Separate unit and integration test execution in CI

**Non-Goals:**
- Load or performance testing
- UI / browser-based end-to-end testing
- Mutation testing
- Changes to production service logic (beyond the `ITimeProvider` abstraction)

## Decisions

### Decision 1: Use `Aspire.Hosting.Testing` for integration tests (not Docker Compose or in-process fakes)

**Chosen**: `Aspire.Hosting.Testing` spins up the actual AppHost, which starts real Dapr sidecars, Redis, and all services in their configured topology.

**Alternatives considered**:
- *In-process fakes for Dapr*: Lightweight but does not exercise sidecar routing, state-store serialisation, or pub/sub fan-out — the exact areas most likely to fail on stage.
- *Docker Compose + HTTP calls*: More realistic but requires external orchestration and is harder to run in CI without privileged containers.

**Rationale**: `Aspire.Hosting.Testing` is the canonical integration path for .NET Aspire projects, provides automatic resource teardown, and keeps everything inside the standard `dotnet test` pipeline.

### Decision 2: `RideWorkflowTestHarness` wrapper around the Dapr workflow test client

**Chosen**: A thin helper class (`RideWorkflowTestHarness`) exposes typed methods (`StartRideAsync`, `GetStateAsync`, `WaitForStateAsync`) that hide Dapr workflow API verbosity.

**Alternatives considered**:
- *Direct Dapr client calls in every test*: Works but leads to copy-paste boilerplate, especially for polling state transitions.

**Rationale**: Keeps test code readable, makes intent obvious, and isolates tests from Dapr SDK changes.

### Decision 3: `Bogus` `Faker<Passenger>` for test data generation

**Chosen**: Each test project uses a shared `PassengerFaker` built with Bogus. It generates realistic names, ages, and ride-pass IDs on every run.

**Alternatives considered**:
- *Hard-coded test objects*: Simple but brittle — tests pass only for the one hard-coded input.
- *AutoFixture*: More powerful but adds a dependency not already in the project.

**Rationale**: Bogus is already included in the test projects. Randomised-but-realistic data catches boundary conditions that fixed values miss.

### Decision 4: `ITimeProvider` abstraction for workflow timeout tests

**Chosen**: Workflow activities that schedule delays (`Task.Delay`) accept an `ITimeProvider` dependency. Tests inject a `FakeTimeProvider` (from `Microsoft.Extensions.TimeProvider.Testing`) that advances time programmatically.

**Alternatives considered**:
- *Real `Task.Delay` in tests*: The maintenance-approval timeout is 30 minutes — unacceptable in CI.
- *Shortened timeout via environment variable*: Fragile and pollutes production config.

**Rationale**: `ITimeProvider` / `FakeTimeProvider` is the idiomatic .NET approach, avoids production config changes, and keeps tests deterministic.

### Decision 5: `[Trait("Category", "Integration")]` to gate integration tests in CI

**Chosen**: Every integration test class is decorated with `[Trait("Category", "Integration")]`. CI has two stages: (1) `dotnet test --filter "Category!=Integration"` for unit tests on every push, (2) `dotnet test --filter "Category=Integration"` on PR merge to main.

**Alternatives considered**:
- *Separate solution file*: Physically separates projects but complicates tooling and local `dotnet build` workflows.

**Rationale**: Single solution, minimal CI configuration, standard xUnit trait mechanism.

## Risks / Trade-offs

- **Dapr sidecar startup time** increases integration-test suite duration (estimated +2–4 min per run). → Mitigation: run integration tests only on PR merge, not on every push.
- **`Aspire.Hosting.Testing` is still evolving** — API surface may change between preview releases. → Mitigation: pin Aspire version in `Directory.Packages.props`; update deliberately.
- **`FakeTimeProvider` advances time globally** in a test — if two workflow instances run concurrently in the same test class they may interfere. → Mitigation: use `IClassFixture` to isolate AppHost per test class; run timeout tests sequentially with `[Collection]`.
- **Bogus randomisation means flaky tests are possible** if a fake value happens to hit a validation boundary. → Mitigation: seed the `Faker` with a fixed value in CI (`BOGUS_SEED` environment variable); default to random locally.

## Open Questions

- Should `RefundAssertions` live in a shared `ThemePark.Tests.Shared` library, or be duplicated into each service test project? (Recommendation: shared library, but needs solution-structure decision.)
- Is `ThemePark.IntegrationTests` a standalone project or does it live under the AppHost solution folder?
