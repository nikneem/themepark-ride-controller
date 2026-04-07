## ADDED Requirements

### Requirement: All rides start in Idle state before the demo sequence
The E2E demo validation SHALL assert that all 5 configured rides are in `RideState.Idle` before any demo action is taken.

#### Scenario: Pre-demo state verified
- **WHEN** the E2E test suite starts
- **THEN** querying the state of all 5 rides SHALL return `RideState.Idle` for each ride

---

### Requirement: Starting Thunder Mountain transitions through PreFlight, Loading, and Running
The E2E test SHALL start the Thunder Mountain ride and assert that it transitions through `PreFlight → Loading → Running` in sequence.

#### Scenario: Thunder Mountain start sequence
- **WHEN** the demo operator issues the `StartRide` command for Thunder Mountain
- **THEN** the ride state SHALL transition to `RideState.PreFlight`
- **THEN** the ride state SHALL transition to `RideState.Loading`
- **THEN** the ride state SHALL transition to `RideState.Running`

---

### Requirement: Mild-weather chaos is triggered and cleared during the demo
The E2E test SHALL inject a mild-weather chaos event while Thunder Mountain is running, verify the ride pauses, then clear the event and verify the ride resumes.

#### Scenario: Mild weather triggered and cleared
- **WHEN** the demo operator triggers mild weather while the ride is `Running`
- **THEN** the ride state SHALL transition to `RideState.Paused`
- **WHEN** the demo operator clears the weather event
- **THEN** the ride state SHALL transition back to `RideState.Running`

---

### Requirement: Mascot-intrusion chaos is triggered and cleared during the demo
The E2E test SHALL inject a mascot-intrusion event while the ride is running, verify the pause, then clear and verify resumption.

#### Scenario: Mascot intrusion triggered and cleared
- **WHEN** the demo operator triggers a mascot intrusion while the ride is `Running`
- **THEN** the ride state SHALL transition to `RideState.Paused`
- **WHEN** the demo operator clears the intrusion
- **THEN** the ride state SHALL transition back to `RideState.Running`

---

### Requirement: Mechanical failure is triggered and resolved via maintenance approval
The E2E test SHALL inject a mechanical-failure event, verify the ride enters `Maintenance`, then approve and complete maintenance, and verify the ride resumes.

#### Scenario: Mechanical failure approved and resolved
- **WHEN** the demo operator triggers a mechanical failure while the ride is `Running`
- **THEN** the ride state SHALL transition to `RideState.Maintenance`
- **WHEN** the demo operator approves maintenance and maintenance is marked completed
- **THEN** the ride state SHALL transition back to `RideState.Running`

---

### Requirement: The ride completes after all chaos events are resolved
The E2E test SHALL wait for the ride to reach `RideState.Completed` after all chaos events have been resolved and the ride timer elapses.

#### Scenario: Ride completes after chaos resolution
- **WHEN** all chaos events have been cleared and the ride timer elapses
- **THEN** the ride state SHALL transition to `RideState.Completed`

---

### Requirement: Ride history shows all events for the completed session
The E2E test SHALL query the ride-history endpoint for Thunder Mountain and verify that all expected state-transition events are present in the session log.

#### Scenario: Full event history is recorded
- **WHEN** the E2E test queries the ride-history endpoint after the ride reaches `Completed`
- **THEN** the response SHALL contain events for `Started`, `PreFlightPassed`, `LoadingStarted`, `RideStarted`, `WeatherMildPaused`, `WeatherCleared`, `MascotIntrusionPaused`, `IntrusionCleared`, `MechanicalFailurePaused`, `MaintenanceApproved`, `MaintenanceCompleted`, and `RideCompleted` in chronological order
