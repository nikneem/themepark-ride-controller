## ADDED Requirements

### Requirement: Subscribe to weather.alert topic
The system SHALL subscribe to the `weather.alert` Dapr pub/sub topic and raise a `WeatherAlertReceived` external event into the active workflow for the affected ride.

#### Scenario: Active workflow exists for affected ride
- **WHEN** a message is published to `weather.alert` containing a `rideId`
- **THEN** the subscriber reads `active-workflow-{rideId}` from the state store and calls `DaprClient.RaiseWorkflowEventAsync` with event name `WeatherAlertReceived`

#### Scenario: No active workflow for affected ride
- **WHEN** a message is published to `weather.alert` and no active workflow exists for the ride
- **THEN** the subscriber logs a warning and returns a success acknowledgement to Dapr (no retry)

### Requirement: Subscribe to mascot.in-restricted-zone topic
The system SHALL subscribe to the `mascot.in-restricted-zone` Dapr pub/sub topic and raise a `MascotIntrusionReceived` external event into the active workflow for the affected ride.

#### Scenario: Active workflow exists for affected ride
- **WHEN** a message is published to `mascot.in-restricted-zone` containing a `rideId`
- **THEN** the subscriber reads `active-workflow-{rideId}` from the state store and calls `DaprClient.RaiseWorkflowEventAsync` with event name `MascotIntrusionReceived`

#### Scenario: No active workflow for affected ride
- **WHEN** a message is published to `mascot.in-restricted-zone` and no active workflow exists
- **THEN** the subscriber logs a warning and acknowledges the message without raising an event

### Requirement: Subscribe to ride.malfunction topic
The system SHALL subscribe to the `ride.malfunction` Dapr pub/sub topic and raise a `MalfunctionReceived` external event into the active workflow for the affected ride.

#### Scenario: Active workflow exists for affected ride
- **WHEN** a message is published to `ride.malfunction` containing a `rideId`
- **THEN** the subscriber reads `active-workflow-{rideId}` from the state store and calls `DaprClient.RaiseWorkflowEventAsync` with event name `MalfunctionReceived`

#### Scenario: No active workflow for affected ride
- **WHEN** a message is published to `ride.malfunction` and no active workflow exists
- **THEN** the subscriber logs a warning and acknowledges the message without raising an event

### Requirement: Subscribe to maintenance.completed topic
The system SHALL subscribe to the `maintenance.completed` Dapr pub/sub topic and raise a `MaintenanceCompleted` external event into the active workflow for the affected ride.

#### Scenario: Active workflow is awaiting maintenance completion
- **WHEN** a message is published to `maintenance.completed` containing a `rideId`
- **THEN** the subscriber reads `active-workflow-{rideId}` from the state store and calls `DaprClient.RaiseWorkflowEventAsync` with event name `MaintenanceCompleted`

#### Scenario: No active workflow for affected ride
- **WHEN** a message is published to `maintenance.completed` and no active workflow exists
- **THEN** the subscriber logs a warning and acknowledges without raising an event

### Requirement: Idempotent event delivery
Pub/sub subscribers SHALL handle duplicate message delivery without producing duplicate workflow events or errors.

#### Scenario: Duplicate pub/sub message received
- **WHEN** the same pub/sub message is delivered more than once (e.g., Dapr at-least-once delivery)
- **THEN** the workflow external event is only raised once; subsequent duplicates are acknowledged and discarded (e.g., via workflow state guard)
