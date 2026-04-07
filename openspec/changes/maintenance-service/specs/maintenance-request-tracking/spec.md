## ADDED Requirements

### Requirement: Create maintenance request
The system SHALL accept a maintenance request via `POST /maintenance/request` with a JSON body containing `rideId`, `reason`, `workflowId`, and `requestedAt`. It SHALL generate a GUID `maintenanceId`, store the record with status `Pending`, and return HTTP 201 with `{ maintenanceId, rideId, status: "Pending" }`.

#### Scenario: Successful maintenance request creation
- **WHEN** a caller sends `POST /maintenance/request` with a valid body `{ rideId, reason, workflowId, requestedAt }`
- **THEN** the system stores a new maintenance record with status `Pending`
- **AND** returns HTTP 201 with `{ maintenanceId, rideId, status: "Pending" }`
- **AND** `maintenanceId` is a non-empty GUID

#### Scenario: Missing required field returns 400
- **WHEN** a caller sends `POST /maintenance/request` with a missing `rideId` or `reason`
- **THEN** the system returns HTTP 400 Bad Request

### Requirement: Valid maintenance reasons
The system SHALL accept only the following `reason` values: `MechanicalFailure`, `ScheduledCheck`, `Failure`. Any other value SHALL result in HTTP 400.

#### Scenario: Valid reason accepted
- **WHEN** `reason` is `MechanicalFailure`, `ScheduledCheck`, or `Failure`
- **THEN** the request is accepted and returns HTTP 201

#### Scenario: Invalid reason rejected
- **WHEN** `reason` is any value not in the allowed list
- **THEN** the system returns HTTP 400 Bad Request

### Requirement: Complete a maintenance request
The system SHALL mark a maintenance request as `Completed` via `POST /maintenance/{maintenanceId}/complete`. It SHALL update the record's status to `Completed`, record `completedAt`, and return HTTP 200 with `{ maintenanceId, status: "Completed", completedAt }`.

#### Scenario: Successful completion
- **WHEN** a caller sends `POST /maintenance/{maintenanceId}/complete` for a record with status `Pending` or `InProgress`
- **THEN** the system updates the record status to `Completed` and sets `completedAt` to the current UTC time
- **AND** returns HTTP 200 with `{ maintenanceId, status: "Completed", completedAt }`

#### Scenario: Complete non-existent record returns 404
- **WHEN** a caller sends `POST /maintenance/{maintenanceId}/complete` and no record exists for that `maintenanceId`
- **THEN** the system returns HTTP 404 Not Found

#### Scenario: Complete already-completed record returns 409
- **WHEN** a caller sends `POST /maintenance/{maintenanceId}/complete` for a record already in `Completed` or `Cancelled` status
- **THEN** the system returns HTTP 409 Conflict

### Requirement: Status transitions are server-enforced
The system SHALL enforce the following status transitions: `Pending → InProgress`, `InProgress → Completed`, and any non-terminal state → `Cancelled`. Transitions that violate this model SHALL return HTTP 409 Conflict.

#### Scenario: Linear happy-path transition
- **WHEN** the system completes a `Pending` request
- **THEN** status transitions directly to `Completed`

#### Scenario: Illegal transition rejected
- **WHEN** a caller attempts to complete a record already in `Cancelled` status
- **THEN** the system returns HTTP 409 Conflict

### Requirement: Retrieve maintenance history for a ride
The system SHALL return the last 20 maintenance records for a given `rideId` via `GET /maintenance/{rideId}/history`. Records SHALL be ordered with the most recent first. Each record SHALL include `maintenanceId`, `reason`, `status`, `requestedAt`, `completedAt` (nullable), and `durationMinutes` (nullable, derived from `requestedAt` and `completedAt` when both are present).

#### Scenario: History returned for ride with records
- **WHEN** a caller sends `GET /maintenance/{rideId}/history` for a ride that has maintenance records
- **THEN** the system returns HTTP 200 with an array of up to 20 records, most recent first
- **AND** each record contains `maintenanceId`, `reason`, `status`, `requestedAt`, `completedAt`, and `durationMinutes`

#### Scenario: History capped at 20 records
- **WHEN** a ride has more than 20 maintenance records
- **THEN** only the 20 most recent records are returned

#### Scenario: Empty history for unknown ride
- **WHEN** a caller sends `GET /maintenance/{rideId}/history` for a ride with no records
- **THEN** the system returns HTTP 200 with an empty array

#### Scenario: durationMinutes calculated from timestamps
- **WHEN** a completed record has both `requestedAt` and `completedAt`
- **THEN** `durationMinutes` is the integer number of minutes between `requestedAt` and `completedAt`

#### Scenario: durationMinutes is null for incomplete records
- **WHEN** a record has status `Pending` or `InProgress` (no `completedAt`)
- **THEN** `durationMinutes` is `null`
