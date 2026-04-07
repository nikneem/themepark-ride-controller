## ADDED Requirements

### Requirement: Publish maintenance.requested event on request creation
The system SHALL publish a `maintenance.requested` event via Dapr pub/sub immediately after a maintenance request record is persisted. The event payload SHALL include `{ maintenanceId, rideId, reason, workflowId, requestedAt }`.

#### Scenario: Event published on successful request creation
- **WHEN** `POST /maintenance/request` succeeds and the record is persisted
- **THEN** a `maintenance.requested` event is published to the Dapr pub/sub topic
- **AND** the payload contains `maintenanceId`, `rideId`, `reason`, `workflowId`, and `requestedAt`

#### Scenario: Event not published when request creation fails
- **WHEN** `POST /maintenance/request` fails due to a validation error or state-store error
- **THEN** no `maintenance.requested` event is published

### Requirement: Publish maintenance.completed event on completion
The system SHALL publish a `maintenance.completed` event via Dapr pub/sub immediately after a maintenance record is marked `Completed`. The event payload SHALL include `{ maintenanceId, rideId, completedAt }` so that subscribers can correlate the event with the correct workflow instance.

#### Scenario: Event published on successful completion
- **WHEN** `POST /maintenance/{maintenanceId}/complete` succeeds and the record is updated to `Completed`
- **THEN** a `maintenance.completed` event is published to the Dapr pub/sub topic
- **AND** the payload contains `maintenanceId`, `rideId`, and `completedAt`

#### Scenario: rideId is present in maintenance.completed payload
- **WHEN** the `maintenance.completed` event is published
- **THEN** the payload includes the `rideId` of the completed maintenance record
- **AND** subscribers can use `rideId` to route the event to the correct Dapr Workflow instance without additional lookups

#### Scenario: Event not published when completion fails
- **WHEN** `POST /maintenance/{maintenanceId}/complete` returns 404 or 409
- **THEN** no `maintenance.completed` event is published

### Requirement: Events published at-least-once
The system SHALL use Dapr pub/sub with at-least-once delivery semantics. Consumers of `maintenance.completed` SHALL be idempotent with respect to duplicate events.

#### Scenario: Workflow resumes on maintenance.completed receipt
- **WHEN** a `maintenance.completed` event is received by the ride-controller workflow subscriber
- **THEN** the workflow's `WaitForExternalEvent` resolves and the workflow resumes
- **AND** if the event is received a second time (duplicate), the workflow does not fail
