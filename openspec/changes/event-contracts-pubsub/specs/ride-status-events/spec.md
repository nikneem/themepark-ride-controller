## ADDED Requirements

### Requirement: ride.status-changed event contract
The system SHALL define a `RideStatusChangedEvent` record with fields `RideId` (Guid), `PreviousStatus` (string), `NewStatus` (string), `WorkflowStep` (string), and `ChangedAt` (DateTimeOffset), published on topic `ride.status-changed` by Control Center API on every ride status transition in RideWorkflow.

#### Scenario: Event published on every ride status transition
- **WHEN** RideWorkflow in Control Center API transitions a ride to a new status
- **THEN** a `RideStatusChangedEvent` is published to topic `ride.status-changed` on component `themepark-pubsub` with the ride ID, the previous status, the new status, the name of the current workflow step, and the current UTC timestamp in `changedAt`

#### Scenario: PreviousStatus and NewStatus are different
- **WHEN** a `RideStatusChangedEvent` is published
- **THEN** `previousStatus` and `newStatus` are different string values reflecting the actual state transition

#### Scenario: WorkflowStep identifies the workflow activity
- **WHEN** a `RideStatusChangedEvent` is published from within RideWorkflow
- **THEN** `workflowStep` contains the name of the workflow activity or step that caused the transition (e.g., `"PauseRide"`, `"ResumeRide"`, `"EnterMaintenance"`)

---

### Requirement: ride.status-changed forwarded to frontend via SSE
Control Center API SHALL forward every `ride.status-changed` event to all connected frontend clients via a Server-Sent Events (SSE) stream so that the frontend receives real-time ride status updates without polling.

#### Scenario: SSE clients receive status change immediately
- **WHEN** Control Center API publishes a `RideStatusChangedEvent`
- **THEN** all clients currently connected to the SSE endpoint receive the event payload within the same request cycle

#### Scenario: SSE event payload is the serialised RideStatusChangedEvent
- **WHEN** a `RideStatusChangedEvent` is forwarded to the SSE stream
- **THEN** the SSE `data` field contains the JSON-serialised event with camelCase property names

#### Scenario: No Dapr subscriber for ride.status-changed in Control Center
- **WHEN** Control Center API is running
- **THEN** there is no Dapr subscription registered for topic `ride.status-changed` in Control Center API — it is the publisher, not a subscriber

---

### Requirement: ride.status-changed dead letter topic
The system SHALL configure a dead letter topic `ride.status-changed.deadletter` for any downstream service that subscribes to `ride.status-changed`, even though the primary consumer is the SSE push path, to maintain consistency with the infrastructure requirement.

#### Scenario: Dead letter topic name follows convention
- **WHEN** a downstream service subscribes to topic `ride.status-changed`
- **THEN** its subscription declaration specifies `ride.status-changed.deadletter` as the `deadLetterTopic`

---

### Requirement: RideStatus values are well-known strings
The `PreviousStatus` and `NewStatus` fields in `RideStatusChangedEvent` SHALL use a well-known set of string values aligned with the RideWorkflow state machine so that the frontend can display meaningful status labels.

#### Scenario: Known status strings used in events
- **WHEN** RideWorkflow publishes a `RideStatusChangedEvent`
- **THEN** both `previousStatus` and `newStatus` are one of the values defined in the ride status enumeration (e.g., `"Operational"`, `"Paused"`, `"Maintenance"`, `"Fault"`, `"Closed"`)

#### Scenario: Unknown status string does not crash subscriber
- **WHEN** a `RideStatusChangedEvent` with an unrecognised `newStatus` string is received by a subscriber
- **THEN** the subscriber logs a warning and acknowledges the message rather than returning an error
