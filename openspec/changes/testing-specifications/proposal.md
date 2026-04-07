## Why

Without a formalised testing strategy, each developer writes tests inconsistently and critical workflow scenarios (compensation, timeout escalation, chaos event handling) go uncovered. Formalising the strategy before a live conference demo ensures every ride state-transition is reliably exercised and the demo does not fail on stage.

## What Changes

- Introduce a standardised unit-test strategy across all 7 microservices — naming conventions, coverage targets, Moq mock patterns, and Bogus fake-data generation
- Add an integration-test harness built on `Aspire.Hosting.Testing` that exercises the full Aspire AppHost (real Dapr sidecars + Redis) for 6 key ride-workflow scenarios
- Add an end-to-end demo-validation suite mirroring the 7-step conference demo script, serving as an automated acceptance gate before each live presentation

## Capabilities

### New Capabilities

- `unit-test-strategy`: Naming conventions (`MethodName_Scenario_ExpectedBehavior`), per-layer coverage targets (handlers ≥ 90 %, domain/state transitions 100 %, workflow activities ≥ 80 %), Moq dependency mocking patterns, and Bogus `Faker<Passenger>` data generation
- `integration-test-harness`: `Aspire.Hosting.Testing` project setup, `RideWorkflowTestHarness`, `ChaosEventInjector`, and the 6 workflow integration scenarios (happy path, mild weather, mascot intrusion, mechanical failure, severe-weather pre-flight, maintenance timeout)
- `e2e-demo-validation`: Sequential 7-step acceptance test that mirrors the exact conference demo script and gates every release

### Modified Capabilities

## Impact

- New project `ThemePark.IntegrationTests` added to the solution
- All 7 existing `ThemePark.<Service>.Tests` projects gain shared test conventions and Bogus/Moq helpers
- CI pipeline updated to run unit tests and integration tests in separate stages (`[Trait("Category", "Integration")]` tag gates integration tests)
- `ITimeProvider` abstraction added to workflow activities so timeout tests avoid real 30-minute waits
- No production API or data-model changes; test-only additions
