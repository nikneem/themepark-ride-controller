## ADDED Requirements

### Requirement: Integration tests use Aspire.Hosting.Testing to start the full AppHost
The `ThemePark.IntegrationTests` project SHALL use `Aspire.Hosting.Testing` to start the full Aspire AppHost, including all 7 microservices, Dapr sidecars, and Redis, for every integration test class.

#### Scenario: AppHost starts successfully before tests run
- **WHEN** the integration test runner initialises the `DistributedApplicationTestingBuilder`
- **THEN** all Aspire resources SHALL reach a healthy state before the first test method executes

#### Scenario: Resources are torn down after tests complete
- **WHEN** the last test in an integration test class finishes
- **THEN** the AppHost SHALL shut down and all containers SHALL be removed automatically

---

### Requirement: Integration tests are tagged with the Integration category trait
Every integration test class SHALL be decorated with `[Trait("Category", "Integration")]` so they can be excluded from or included in a test run by filter.

#### Scenario: Unit tests run without starting AppHost
- **WHEN** the CI pipeline runs `dotnet test --filter "Category!=Integration"`
- **THEN** no integration test class SHALL be initialised and the AppHost SHALL not start

#### Scenario: Integration tests run in isolation
- **WHEN** the CI pipeline runs `dotnet test --filter "Category=Integration"`
- **THEN** only integration test classes SHALL execute

---

### Requirement: Happy-path workflow scenario completes successfully
The integration harness SHALL verify that a ride workflow started with valid passengers and no chaos events transitions through `PreFlight → Loading → Running → Completed`.

#### Scenario: Ride completes without chaos
- **WHEN** a ride workflow is started via `RideWorkflowTestHarness.StartRideAsync` with 20 valid passengers and no chaos events are injected
- **THEN** the workflow SHALL reach `RideState.Completed` within the configured ride duration and all 20 passengers SHALL be recorded as having completed the ride

---

### Requirement: Mild-weather chaos scenario pauses and resumes the ride
The integration harness SHALL verify that a `WeatherMild` chaos event pauses the ride and that an operator `ClearWeather` event resumes it to `Completed`.

#### Scenario: Mild weather pauses then clears
- **WHEN** a running ride receives a `WeatherMild` event injected via `ChaosEventInjector`
- **THEN** the workflow state SHALL transition to `RideState.Paused`
- **WHEN** an operator sends a `ClearWeather` event
- **THEN** the workflow state SHALL transition back to `RideState.Running` and eventually to `RideState.Completed`

---

### Requirement: Mascot-intrusion chaos scenario pauses and resumes the ride
The integration harness SHALL verify that a `MascotIntrusion` chaos event pauses the ride and that an operator `ClearIntrusion` event resumes it.

#### Scenario: Mascot intrusion pauses then clears
- **WHEN** a running ride receives a `MascotIntrusion` event injected via `ChaosEventInjector`
- **THEN** the workflow state SHALL transition to `RideState.Paused`
- **WHEN** an operator sends a `ClearIntrusion` event
- **THEN** the workflow state SHALL transition back to `RideState.Running` and eventually to `RideState.Completed`

---

### Requirement: Mechanical-failure chaos scenario routes through maintenance approval
The integration harness SHALL verify that a `MechanicalFailure` event transitions the ride to `Maintenance`, that an operator approval plus `maintenance.completed` event resumes it, and that the ride finishes normally.

#### Scenario: Mechanical failure resolved by maintenance team
- **WHEN** a running ride receives a `MechanicalFailure` event via `ChaosEventInjector`
- **THEN** the workflow state SHALL transition to `RideState.Maintenance`
- **WHEN** an operator sends `ApproveMaintenance` followed by a `maintenance.completed` event
- **THEN** the workflow state SHALL transition to `RideState.Running` and eventually to `RideState.Completed`

---

### Requirement: Severe-weather pre-flight failure issues refunds
The integration harness SHALL verify that when severe weather is present during `PreFlight` the workflow transitions to `RideState.Failed` and refunds are issued for all loaded passengers.

#### Scenario: Severe weather detected at pre-flight
- **WHEN** a ride workflow starts and a `WeatherSevere` event is injected during the `PreFlight` phase
- **THEN** the workflow state SHALL transition to `RideState.Failed`
- **THEN** `RefundAssertions.AssertAllPassengersRefunded` SHALL pass for all passengers registered for that ride session

---

### Requirement: Maintenance-approval timeout transitions to Failed and issues refunds
The integration harness SHALL verify that if `MaintenanceApproved` is not received within the configured timeout window the workflow transitions to `RideState.Failed` and issues refunds.

#### Scenario: Maintenance approval times out
- **WHEN** a ride is in `RideState.Maintenance` and the `FakeTimeProvider` advances time beyond the approval timeout without an `ApproveMaintenance` event
- **THEN** the workflow state SHALL transition to `RideState.Failed`
- **THEN** `RefundAssertions.AssertAllPassengersRefunded` SHALL pass for all passengers registered for that ride session

---

### Requirement: RideWorkflowTestHarness provides typed workflow control
The `RideWorkflowTestHarness` class SHALL expose typed methods for starting a ride workflow, querying its current state, and waiting for a target state with a configurable timeout.

#### Scenario: Start ride and wait for Running state
- **WHEN** a test calls `harness.StartRideAsync(rideId, passengers)` followed by `harness.WaitForStateAsync(RideState.Running, timeout)`
- **THEN** the method SHALL return without throwing if the workflow reaches `Running` within the timeout

#### Scenario: WaitForState times out and throws
- **WHEN** a test calls `harness.WaitForStateAsync(RideState.Running, TimeSpan.FromSeconds(5))` and the workflow never reaches `Running`
- **THEN** the method SHALL throw a `WorkflowStateTimeoutException` with a message identifying the ride and the expected state

---

### Requirement: ChaosEventInjector fires external events into running workflows
The `ChaosEventInjector` class SHALL expose typed methods for sending each supported chaos event (`WeatherMild`, `WeatherSevere`, `MascotIntrusion`, `MechanicalFailure`) and operator responses (`ClearWeather`, `ClearIntrusion`, `ApproveMaintenance`) to a running workflow instance.

#### Scenario: Chaos event is delivered to the correct workflow instance
- **WHEN** a test calls `injector.InjectAsync(rideId, ChaosEvent.MascotIntrusion)`
- **THEN** the Dapr workflow instance identified by `rideId` SHALL receive the external event and react accordingly
