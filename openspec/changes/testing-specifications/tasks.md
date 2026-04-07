## 1. Shared Test Infrastructure

- [ ] 1.1 Create `ThemePark.Tests.Shared` class library project and add it to the solution
- [ ] 1.2 Add Bogus NuGet package to `ThemePark.Tests.Shared` and implement `PassengerFaker` with name, age [5–90], and unique `RidePassId` generation; support `BOGUS_SEED` environment variable for deterministic CI output
- [ ] 1.3 Implement `RefundAssertions` custom xUnit assertion helper in `ThemePark.Tests.Shared` with `AssertAllPassengersRefunded(IEnumerable<Passenger> passengers, IEnumerable<RefundRecord> refunds)` method
- [ ] 1.4 Add `ThemePark.Tests.Shared` project reference to all 7 `ThemePark.<Service>.Tests` projects

## 2. ITimeProvider Abstraction

- [ ] 2.1 Define `ITimeProvider` interface with `Task DelayAsync(TimeSpan duration, CancellationToken ct)` in the appropriate shared production library
- [ ] 2.2 Implement `SystemTimeProvider` (production implementation wrapping `Task.Delay`) and register it in each service's DI container
- [ ] 2.3 Implement `FakeTimeProvider` in `ThemePark.Tests.Shared` using `Microsoft.Extensions.TimeProvider.Testing` that advances time programmatically for use in timeout tests

## 3. Integration Test Project

- [ ] 3.1 Create `ThemePark.IntegrationTests` xUnit project and add it to the solution
- [ ] 3.2 Add `Aspire.Hosting.Testing`, Moq, and Bogus NuGet packages to `ThemePark.IntegrationTests`; add reference to `ThemePark.Tests.Shared`
- [ ] 3.3 Implement `RideWorkflowTestHarness` with `StartRideAsync(rideId, passengers)`, `GetStateAsync(rideId)`, and `WaitForStateAsync(rideId, targetState, timeout)` (throws `WorkflowStateTimeoutException` on timeout)
- [ ] 3.4 Implement `ChaosEventInjector` with typed methods for `WeatherMild`, `WeatherSevere`, `MascotIntrusion`, `MechanicalFailure`, `ClearWeather`, `ClearIntrusion`, and `ApproveMaintenance` events

## 4. Integration Test Scenarios

- [ ] 4.1 Implement happy-path integration test: start ride with 20 passengers, assert `PreFlight → Loading → Running → Completed`, verify all 20 passengers recorded; tag `[Trait("Category", "Integration")]`
- [ ] 4.2 Implement mild-weather integration test: inject `WeatherMild` while running, assert `Paused`, clear weather, assert `Running → Completed`; tag `[Trait("Category", "Integration")]`
- [ ] 4.3 Implement mascot-intrusion integration test: inject `MascotIntrusion` while running, assert `Paused`, clear intrusion, assert `Running → Completed`; tag `[Trait("Category", "Integration")]`
- [ ] 4.4 Implement mechanical-failure integration test: inject `MechanicalFailure`, assert `Maintenance`, send `ApproveMaintenance` + `maintenance.completed`, assert `Running → Completed`; tag `[Trait("Category", "Integration")]`
- [ ] 4.5 Implement severe-weather pre-flight integration test: inject `WeatherSevere` during `PreFlight`, assert `Failed`, run `RefundAssertions.AssertAllPassengersRefunded`; tag `[Trait("Category", "Integration")]`
- [ ] 4.6 Implement maintenance-timeout integration test: enter `Maintenance`, advance `FakeTimeProvider` beyond approval timeout without sending `ApproveMaintenance`, assert `Failed`, run `RefundAssertions.AssertAllPassengersRefunded`; tag `[Trait("Category", "Integration")]`

## 5. E2E Demo Validation Tests

- [ ] 5.1 Implement E2E test step 1: assert all 5 rides are in `RideState.Idle` at suite start; tag `[Trait("Category", "Integration")]`
- [ ] 5.2 Implement E2E test steps 2–5: start Thunder Mountain, assert `PreFlight → Loading → Running`, trigger mild weather, assert `Paused → Running`, trigger mascot intrusion, assert `Paused → Running`, trigger mechanical failure, assert `Maintenance`, approve, assert `Running`
- [ ] 5.3 Implement E2E test steps 6–7: wait for `Completed`, query ride-history endpoint, assert all 12 expected events are present in chronological order

## 6. Unit Test Conventions per Service

- [ ] 6.1 Add unit tests for all command and query handlers in `ThemePark.Rides.Tests` using Moq for dependencies and `PassengerFaker` for data; achieve ≥ 90 % handler coverage and 100 % domain/state-transition coverage
- [ ] 6.2 Add unit tests for workflow activities in `ThemePark.Rides.Tests` using mocked Dapr client; achieve ≥ 80 % activity coverage
- [ ] 6.3 Repeat handler, domain, and activity unit tests for all remaining 6 services (`Boarding`, `Passengers`, `Refunds`, `Notifications`, `Weather`, `Operator`) following the same coverage targets and naming convention

## 7. CI Pipeline Configuration

- [ ] 7.1 Update CI pipeline to add a `unit-tests` stage that runs `dotnet test --filter "Category!=Integration"` on every push to any branch
- [ ] 7.2 Update CI pipeline to add an `integration-tests` stage that runs `dotnet test --filter "Category=Integration"` only on pull-request merge to `main`
- [ ] 7.3 Configure coverage reporting in CI: fail the `unit-tests` stage if handler coverage < 90 %, domain coverage < 100 %, or activity coverage < 80 %
