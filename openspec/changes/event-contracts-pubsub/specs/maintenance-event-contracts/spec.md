## ADDED Requirements

### Requirement: maintenance.requested event contract
The system SHALL define a `MaintenanceRequestedEvent` record with fields `EventId` (Guid), `MaintenanceId` (string), `RideId` (Guid), `Reason` (string), and `RequestedAt` (DateTimeOffset), published on topic `maintenance.requested` by Maintenance Service when a maintenance request is created.

#### Scenario: Maintenance requested event published
- **WHEN** Maintenance Service receives `POST /maintenance/request`
- **THEN** it publishes a `MaintenanceRequestedEvent` to topic `maintenance.requested` on component `themepark-pubsub` with a unique maintenance identifier, the ride ID requiring maintenance, the reason for the request, and the current UTC timestamp in `requestedAt`

#### Scenario: Control Center receives maintenance requested event
- **WHEN** Control Center API receives a message on topic `maintenance.requested`
- **THEN** it deserializes the message into a `MaintenanceRequestedEvent` record and forwards a real-time notification to connected frontend clients via the SSE stream

#### Scenario: Malformed maintenance requested event routed to dead letter
- **WHEN** a message on topic `maintenance.requested` cannot be deserialized into `MaintenanceRequestedEvent`
- **THEN** the subscriber returns a non-200 response and Dapr routes the message to `maintenance.requested.deadletter`

---

### Requirement: maintenance.completed event contract
The system SHALL define a `MaintenanceCompletedEvent` record with fields `EventId` (Guid), `MaintenanceId` (string), `RideId` (Guid), and `CompletedAt` (DateTimeOffset), published on topic `maintenance.completed` by Maintenance Service when a maintenance job is finished.

#### Scenario: Maintenance completed event published
- **WHEN** Maintenance Service receives `POST /maintenance/{id}/complete`
- **THEN** it publishes a `MaintenanceCompletedEvent` to topic `maintenance.completed` on component `themepark-pubsub` with the maintenance identifier, the ride ID, and the current UTC timestamp in `completedAt`

#### Scenario: Control Center unblocks ride workflow on completion
- **WHEN** Control Center API receives a message on topic `maintenance.completed`
- **THEN** it deserializes the message into a `MaintenanceCompletedEvent` record and signals the waiting Dapr workflow step so the ride can transition out of the maintenance state

#### Scenario: Malformed maintenance completed event routed to dead letter
- **WHEN** a message on topic `maintenance.completed` cannot be deserialized into `MaintenanceCompletedEvent`
- **THEN** the subscriber returns a non-200 response and Dapr routes the message to `maintenance.completed.deadletter`

---

### Requirement: MaintenanceId is a non-empty string
The `MaintenanceId` field in both `MaintenanceRequestedEvent` and `MaintenanceCompletedEvent` SHALL be a non-empty string that uniquely identifies the maintenance job so that Control Center can correlate the request and completion events.

#### Scenario: MaintenanceId present in published event
- **WHEN** Maintenance Service publishes a `MaintenanceRequestedEvent`
- **THEN** `maintenanceId` in the JSON payload is a non-empty string

#### Scenario: MaintenanceId matches between request and completion
- **WHEN** Maintenance Service publishes a `MaintenanceCompletedEvent` for a previously requested job
- **THEN** the `maintenanceId` value matches the one published in the corresponding `MaintenanceRequestedEvent`
