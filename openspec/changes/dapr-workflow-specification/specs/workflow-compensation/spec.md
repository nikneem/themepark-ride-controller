## ADDED Requirements

### Requirement: Failure path pauses the ride if it is running
The system SHALL call `PauseRideActivity` as the first step of the failure path, but only if the ride was successfully started (i.e., `StartRideActivity` completed before the failure was triggered). If the ride was never started, `PauseRideActivity` SHALL be skipped.

#### Scenario: Ride is running when failure is triggered
- **WHEN** the failure path is entered after `StartRideActivity` completed
- **THEN** `PauseRideActivity` is called before any other compensation activities

#### Scenario: Ride was never started when failure is triggered
- **WHEN** the failure path is entered during or before the pre-flight phase
- **THEN** `PauseRideActivity` is skipped and the workflow proceeds directly to refund issuance

---

### Requirement: Passengers receive refunds on failure
The system SHALL call `IssueRefundActivity` as part of the failure path. This activity SHALL call `POST /refunds` via Dapr service invocation to the `refund-service`. The activity SHALL include the VIP flag (recorded during `LoadPassengersActivity`) in the refund request payload, but only if passengers were loaded. If `LoadPassengersActivity` never completed, `IssueRefundActivity` SHALL be skipped.

#### Scenario: Refund issued for loaded passengers
- **WHEN** the failure path is entered after `LoadPassengersActivity` completed
- **THEN** `IssueRefundActivity` is called with the passenger VIP flag and a successful refund request is sent to the `refund-service`

#### Scenario: No refund if passengers were never loaded
- **WHEN** the failure path is entered before `LoadPassengersActivity` completed
- **THEN** `IssueRefundActivity` is skipped

---

### Requirement: Maintenance is requested on failure
The system SHALL call `TriggerMaintenanceActivity` as part of the failure path unless it was already called within the current failure sequence. This activity SHALL call `POST /maintenance/request` via Dapr service invocation to the `maintenance-service`.

#### Scenario: Maintenance requested during failure path
- **WHEN** the failure path is entered and `TriggerMaintenanceActivity` has not already been called in this failure sequence
- **THEN** `TriggerMaintenanceActivity` calls `POST /maintenance/request` and the request is sent to the `maintenance-service`

#### Scenario: Maintenance already triggered — not duplicated
- **WHEN** the failure path is entered after a `MalfunctionReceived` flow that already called `TriggerMaintenanceActivity`
- **THEN** `TriggerMaintenanceActivity` is not called a second time

---

### Requirement: Failure path ends with CompleteRideActivity and Failed status
The system SHALL call `CompleteRideActivity` as the final step of the failure path. After `CompleteRideActivity` completes, the workflow SHALL end with final status `Failed`.

#### Scenario: Failure path completes
- **WHEN** all compensation activities have been executed
- **THEN** `CompleteRideActivity` is called and the workflow ends with final status `Failed`

---

### Requirement: Failure path is triggered by pre-flight check failures
The system SHALL enter the failure path when any pre-flight activity (`CheckRideStatusActivity`, `CheckWeatherActivity`, or `CheckMascotActivity`) throws after exhausting its retry policy.

#### Scenario: Ride status check fails
- **WHEN** `CheckRideStatusActivity` finds the ride is not Idle (or the downstream call fails after retries)
- **THEN** the workflow enters the failure path

#### Scenario: Parallel pre-flight fails
- **WHEN** either `CheckWeatherActivity` or `CheckMascotActivity` throws after retries
- **THEN** the fan-out fails, the workflow enters the failure path, and the final workflow state is `Failed`

---

### Requirement: Failure path is triggered by timeout expiry during wait states
The system SHALL enter the failure path when the following timeouts expire without the expected event being received:
- `WeatherCleared` wait: 10 minutes after a Mild weather alert
- `MaintenanceApproved` wait: 30 minutes after a malfunction

#### Scenario: Weather cleared wait times out
- **WHEN** the workflow is waiting for `ChaosEventResolved` after a Mild weather alert and 10 minutes elapse without the event
- **THEN** the workflow enters the failure path and the final workflow state is `Failed`

#### Scenario: Maintenance approval times out
- **WHEN** the workflow is waiting for `MaintenanceApproved` after a malfunction and 30 minutes elapse without the event
- **THEN** the workflow enters the failure path and the final workflow state is `Failed`
