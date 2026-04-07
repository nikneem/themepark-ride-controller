## ADDED Requirements

### Requirement: Test naming follows MethodName_Scenario_ExpectedBehavior convention
All test methods in every `ThemePark.<Service>.Tests` project SHALL follow the naming pattern `MethodName_Scenario_ExpectedBehavior`, where *MethodName* is the production method under test, *Scenario* describes the input or state, and *ExpectedBehavior* describes the observable outcome.

#### Scenario: Correctly named test is accepted
- **WHEN** a test method is named `Handle_ValidCommand_ReturnsSuccessResult`
- **THEN** the name unambiguously identifies the method (`Handle`), the scenario (`ValidCommand`), and the expected behavior (`ReturnsSuccessResult`)

#### Scenario: Incorrectly named test is rejected in code review
- **WHEN** a test method is named `Test1` or `HandleTest`
- **THEN** the pull-request review checklist SHALL flag the name as non-compliant and require a rename before merge

---

### Requirement: Command and Query handler coverage is at least 90 percent
Every command handler and query handler class in a service SHALL have test coverage of at least 90 % of its branches and lines, as measured by the project's coverage report.

#### Scenario: Handler with full happy-path and error-path tests meets target
- **WHEN** a command handler has tests for the success case, a validation-failure case, and a dependency-throws case
- **THEN** the coverage report SHALL show ≥ 90 % for that handler class

#### Scenario: Handler missing error-path tests falls below target
- **WHEN** a command handler has only a happy-path test and no error-path test
- **THEN** the coverage report SHALL show < 90 % and the CI coverage gate SHALL fail

---

### Requirement: Domain model and state-transition coverage is 100 percent
All domain model classes and state-machine transition methods SHALL have 100 % line and branch coverage.

#### Scenario: All valid state transitions are exercised
- **WHEN** tests exist for every `RideState` → `RideState` transition defined in the domain model
- **THEN** the coverage report SHALL show 100 % for the state-machine class

#### Scenario: Unreachable guard branch causes coverage failure
- **WHEN** a guard clause in a state transition is never hit by any test
- **THEN** the coverage report SHALL show < 100 % and the CI gate SHALL fail

---

### Requirement: Workflow activity coverage is at least 80 percent
Dapr workflow activity classes SHALL have test coverage of at least 80 % of their branches and lines, with dependencies mocked via Moq.

#### Scenario: Activity with mocked Dapr state store reaches target
- **WHEN** a workflow activity test injects a `Mock<IDaprClient>` and asserts the activity result
- **THEN** the coverage report SHALL show ≥ 80 % for that activity class

#### Scenario: Activity with no tests falls below target
- **WHEN** no tests exist for a workflow activity
- **THEN** the coverage report SHALL show 0 % and the CI gate SHALL fail

---

### Requirement: Moq is used for all external dependency mocking
Tests SHALL use Moq to mock all external dependencies, including `ILogger<T>`, Dapr state-store clients, pub/sub publishers, and any other interface not under test.

#### Scenario: Logger is mocked with Moq
- **WHEN** a test constructs a command handler that requires `ILogger<T>`
- **THEN** the test SHALL pass `new Mock<ILogger<T>>().Object` (or a `MockLogger` wrapper) rather than a real logger or `NullLogger`

#### Scenario: Dapr state store is mocked with Moq
- **WHEN** a test exercises a query handler that reads from the Dapr state store
- **THEN** the test SHALL use `Mock<IDaprClient>.Setup(...)` to control the returned state and SHALL assert the handler processes it correctly

---

### Requirement: Bogus Faker<Passenger> generates realistic test data
Every test project SHALL reference a shared `PassengerFaker` built with the Bogus library. The faker SHALL generate realistic passenger names, ages between 5 and 90, and unique ride-pass IDs.

#### Scenario: Faker produces valid passenger objects
- **WHEN** a test calls `new PassengerFaker().Generate(10)`
- **THEN** the result SHALL be 10 `Passenger` instances, each with a non-empty name, an age in the range [5, 90], and a unique `RidePassId`

#### Scenario: Faker is seeded for deterministic CI output
- **WHEN** the `BOGUS_SEED` environment variable is set to a numeric value
- **THEN** the `PassengerFaker` SHALL use that value as its random seed and every run SHALL produce identical data

---

### Requirement: Each test project references shared test helpers
Every `ThemePark.<Service>.Tests` project SHALL reference a `ThemePark.Tests.Shared` project that contains `PassengerFaker`, `RefundAssertions`, and any other cross-cutting test utilities.

#### Scenario: Shared helpers are available in service test project
- **WHEN** a test file in `ThemePark.Rides.Tests` uses `PassengerFaker`
- **THEN** the project SHALL compile without errors because `ThemePark.Tests.Shared` is referenced

#### Scenario: Shared helpers are not duplicated across projects
- **WHEN** a developer searches the solution for `PassengerFaker`
- **THEN** the class SHALL exist in exactly one location (`ThemePark.Tests.Shared`)
