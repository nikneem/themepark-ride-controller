## ADDED Requirements

### Requirement: WeatherAlertReceived event triggers pause or failure based on severity
The system SHALL handle a `WeatherAlertReceived` external event during the running loop. If severity is `Mild`, the workflow SHALL pause the ride, wait for a `ChaosEventResolved` event (up to 10 minutes), resume the ride, and re-enter the running loop. If severity is `Severe`, the workflow SHALL enter the failure path immediately.

#### Scenario: Mild weather alert — pause, wait, resume
- **WHEN** a `WeatherAlertReceived` event is raised with severity `Mild`
- **THEN** the workflow calls `PauseRideActivity`, waits for `ChaosEventResolved` (up to 10 minutes), calls `ResumeRideActivity`, and re-enters the running loop

#### Scenario: Severe weather alert — failure path
- **WHEN** a `WeatherAlertReceived` event is raised with severity `Severe`
- **THEN** the workflow exits the loop and enters the failure path

#### Scenario: Mild weather wait times out with no resolution
- **WHEN** a `WeatherAlertReceived` event with severity `Mild` is received and `ChaosEventResolved` is not raised within 10 minutes
- **THEN** the workflow treats the wait as expired, enters the failure path, and the final workflow state is `Failed`

---

### Requirement: MascotIntrusionReceived event triggers pause-and-wait with auto-resolve
The system SHALL handle a `MascotIntrusionReceived` external event during the running loop. The workflow SHALL pause the ride, wait for a `ChaosEventResolved` event. If `ChaosEventResolved` is not raised within 5 minutes, the workflow SHALL treat the mascot as cleared, resume the ride, and re-enter the running loop.

#### Scenario: Mascot intrusion — operator resolves within timeout
- **WHEN** a `MascotIntrusionReceived` event is raised and `ChaosEventResolved` is raised within 5 minutes
- **THEN** the workflow calls `PauseRideActivity`, receives `ChaosEventResolved`, calls `ResumeRideActivity`, and re-enters the running loop

#### Scenario: Mascot intrusion — auto-resolved after 5-minute timeout
- **WHEN** a `MascotIntrusionReceived` event is raised and `ChaosEventResolved` is not raised within 5 minutes
- **THEN** the workflow auto-resolves (treats mascot as cleared), calls `ResumeRideActivity`, and re-enters the running loop

---

### Requirement: MalfunctionReceived event triggers maintenance flow
The system SHALL handle a `MalfunctionReceived` external event during the running loop. The workflow SHALL call `TriggerMaintenanceActivity`, wait for a `MaintenanceApproved` event (up to 30 minutes), and if approved, wait for a `ChaosEventResolved` event (maintenance completed). After both events are received, the workflow SHALL resume the ride and re-enter the running loop.

#### Scenario: Malfunction — maintenance approved and completed
- **WHEN** a `MalfunctionReceived` event is raised, `MaintenanceApproved` is raised within 30 minutes, and `ChaosEventResolved` is raised afterward
- **THEN** the workflow calls `TriggerMaintenanceActivity`, waits for `MaintenanceApproved`, waits for `ChaosEventResolved`, calls `ResumeRideActivity`, and re-enters the running loop

#### Scenario: Malfunction — maintenance approval times out
- **WHEN** a `MalfunctionReceived` event is raised and `MaintenanceApproved` is not raised within 30 minutes
- **THEN** the workflow enters the failure path and the final workflow state is `Failed`

---

### Requirement: MaintenanceApproved event is raised via operator endpoint
The system SHALL expose a `POST /api/rides/{rideId}/maintenance/approve` endpoint that raises a `MaintenanceApproved` external event on the running workflow instance for the specified ride. The endpoint SHALL return HTTP 202 Accepted.

#### Scenario: Maintenance approved by operator
- **WHEN** a POST request is made to `/api/rides/{rideId}/maintenance/approve`
- **THEN** the system raises a `MaintenanceApproved` event on the workflow instance and returns HTTP 202

---

### Requirement: ChaosEventResolved event is raised via operator endpoint
The system SHALL expose a `POST /api/rides/{rideId}/events/{eventId}/resolve` endpoint that raises a `ChaosEventResolved` external event on the running workflow instance for the specified ride. The endpoint SHALL return HTTP 202 Accepted.

#### Scenario: Chaos event resolved by operator
- **WHEN** a POST request is made to `/api/rides/{rideId}/events/{eventId}/resolve`
- **THEN** the system raises a `ChaosEventResolved` event on the workflow instance and returns HTTP 202

---

### Requirement: Pub/sub subscriptions raise external events into the workflow
The system SHALL subscribe to the `weather.alert`, `mascot.in-restricted-zone`, and `ride.malfunction` topics. Each subscription handler SHALL call `DaprClient.RaiseWorkflowEventAsync` to inject the corresponding external event (`WeatherAlertReceived`, `MascotIntrusionReceived`, `MalfunctionReceived`) into the running workflow instance for the affected ride.

#### Scenario: Weather alert published — event raised in workflow
- **WHEN** a message is published on the `weather.alert` topic
- **THEN** the subscription handler raises a `WeatherAlertReceived` event (including severity) on the corresponding workflow instance

#### Scenario: Mascot intrusion published — event raised in workflow
- **WHEN** a message is published on the `mascot.in-restricted-zone` topic
- **THEN** the subscription handler raises a `MascotIntrusionReceived` event on the corresponding workflow instance

#### Scenario: Ride malfunction published — event raised in workflow
- **WHEN** a message is published on the `ride.malfunction` topic
- **THEN** the subscription handler raises a `MalfunctionReceived` event on the corresponding workflow instance
