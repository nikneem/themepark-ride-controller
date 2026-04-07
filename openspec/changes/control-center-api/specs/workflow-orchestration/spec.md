## ADDED Requirements

### Requirement: RideWorkflow lifecycle
The system SHALL implement `RideWorkflow` as a Dapr Workflow class that orchestrates a complete ride session from pre-flight through post-ride, with support for external event injection and compensation.

#### Scenario: Successful ride session
- **WHEN** `RideWorkflow` is started with a valid `rideId`
- **THEN** it executes pre-flight checks in parallel, transitions to riding state, waits for ride completion, executes post-ride activities, and terminates with `Completed` outcome

#### Scenario: Pre-flight check fails
- **WHEN** any pre-flight activity returns a failure result during fan-out
- **THEN** the workflow runs compensation activities, sets the session outcome to `Aborted`, and terminates

### Requirement: Pre-flight activity fan-out
The workflow SHALL run all pre-flight check activities in parallel (fan-out) before allowing the ride to start.

#### Scenario: All checks pass
- **WHEN** all pre-flight activities (`CheckWeatherActivity`, `CheckMascotZoneActivity`, `CheckMaintenanceStatusActivity`, `CheckSafetySystemsActivity`) return success
- **THEN** the workflow advances to the riding phase

#### Scenario: One or more checks fail
- **WHEN** any pre-flight activity returns a failure within the fan-out
- **THEN** the workflow halts the fan-out, raises a compensation sequence, and does not proceed to the riding phase

### Requirement: External event — WeatherAlertReceived
The workflow SHALL pause the ride if a `WeatherAlertReceived` external event is received, and wait up to 10 minutes for a `WeatherCleared` event.

#### Scenario: Weather clears within timeout
- **WHEN** `WeatherAlertReceived` is raised and `WeatherCleared` is received within 10 minutes
- **THEN** the workflow resumes the ride session

#### Scenario: Weather does not clear within timeout
- **WHEN** `WeatherAlertReceived` is raised and 10 minutes elapse without `WeatherCleared`
- **THEN** the workflow runs compensation and terminates with `AbortedDueToWeather` outcome

### Requirement: External event — MascotIntrusionReceived
The workflow SHALL pause the ride if a `MascotIntrusionReceived` external event is received, and wait up to 5 minutes for a `MascotCleared` event.

#### Scenario: Mascot cleared within timeout
- **WHEN** `MascotIntrusionReceived` is raised and `MascotCleared` is received within 5 minutes
- **THEN** the workflow resumes the ride session

#### Scenario: Mascot not cleared within timeout
- **WHEN** `MascotIntrusionReceived` is raised and 5 minutes elapse without `MascotCleared`
- **THEN** the workflow runs compensation and terminates with `AbortedDueTOMascot` outcome

### Requirement: External event — MalfunctionReceived
The workflow SHALL pause the ride if a `MalfunctionReceived` external event is received, wait for `MaintenanceApproved` (up to 30 minutes), then wait for `maintenance.completed` confirmation.

#### Scenario: Maintenance approved and completed within timeout
- **WHEN** `MalfunctionReceived` is raised, operator approves within 30 minutes, and `maintenance.completed` pub/sub message is received
- **THEN** the workflow resumes the ride session

#### Scenario: Maintenance not approved within timeout
- **WHEN** `MalfunctionReceived` is raised and 30 minutes elapse without `MaintenanceApproved`
- **THEN** the workflow runs compensation and terminates with `AbortedDueToMaintenance` outcome

### Requirement: Workflow activity contracts
Each activity invoked by `RideWorkflow` SHALL have a clearly defined input and output record, and SHALL be idempotent and free of non-deterministic side effects.

#### Scenario: Activity is replayed by Dapr Workflow runtime
- **WHEN** a workflow activity is replayed during Dapr Workflow history reconstruction
- **THEN** the activity produces the same logical output and does not duplicate external side effects

### Requirement: Workflow instance ID storage
When `RideWorkflow` is started, the workflow instance ID SHALL be stored in the Dapr state store under key `active-workflow-{rideId}` so that pub/sub subscribers can raise external events into it.

#### Scenario: Workflow started
- **WHEN** `POST /api/rides/{rideId}/start` triggers a new workflow
- **THEN** the key `active-workflow-{rideId}` is written to the state store with the workflow instance ID before the 202 response is returned

#### Scenario: Workflow terminates
- **WHEN** the workflow reaches a terminal state (Completed, Aborted, or any failure)
- **THEN** the key `active-workflow-{rideId}` is deleted from the state store
