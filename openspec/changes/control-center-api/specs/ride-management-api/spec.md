## ADDED Requirements

### Requirement: List all rides
The API SHALL return a list of all rides with their current status when `GET /api/rides` is called.

#### Scenario: Rides exist
- **WHEN** a client calls `GET /api/rides`
- **THEN** the system returns HTTP 200 with a JSON array of ride summaries, each including `rideId`, `name`, and `status`

#### Scenario: No rides registered
- **WHEN** a client calls `GET /api/rides` and no rides are registered
- **THEN** the system returns HTTP 200 with an empty JSON array

### Requirement: Get ride status
The API SHALL return the full status of a single ride, including active chaos events and current workflow step, when `GET /api/rides/{rideId}/status` is called.

#### Scenario: Ride exists and is active
- **WHEN** a client calls `GET /api/rides/{rideId}/status` for an active ride
- **THEN** the system returns HTTP 200 with a JSON object containing `rideId`, `name`, `status`, `workflowStep`, and `activeChaosEvents` array

#### Scenario: Ride not found
- **WHEN** a client calls `GET /api/rides/{rideId}/status` for a non-existent rideId
- **THEN** the system returns HTTP 404

### Requirement: Start a ride session
The API SHALL start a new ride session and create a Dapr Workflow when `POST /api/rides/{rideId}/start` is called.

#### Scenario: Ride is available and not currently active
- **WHEN** a client calls `POST /api/rides/{rideId}/start` and no active session exists for that ride
- **THEN** the system creates a new Dapr Workflow, stores the workflow instance ID in state store, and returns HTTP 202 Accepted with the `workflowInstanceId`

#### Scenario: Ride session already active
- **WHEN** a client calls `POST /api/rides/{rideId}/start` and an active session already exists
- **THEN** the system returns HTTP 409 Conflict

#### Scenario: Ride not found
- **WHEN** a client calls `POST /api/rides/{rideId}/start` for a non-existent rideId
- **THEN** the system returns HTTP 404

### Requirement: Approve maintenance
The API SHALL allow an operator to approve pending maintenance for a ride, signalling the active workflow, when `POST /api/rides/{rideId}/maintenance/approve` is called.

#### Scenario: Workflow is waiting for maintenance approval
- **WHEN** a client calls `POST /api/rides/{rideId}/maintenance/approve` and the workflow is paused awaiting `MaintenanceApproved`
- **THEN** the system raises the `MaintenanceApproved` external event into the workflow and returns HTTP 202 Accepted

#### Scenario: No active workflow for ride
- **WHEN** a client calls `POST /api/rides/{rideId}/maintenance/approve` and no active workflow exists for that ride
- **THEN** the system returns HTTP 404

### Requirement: Resolve a chaos event
The API SHALL allow an operator to resolve an active chaos event, signalling the workflow, when `POST /api/rides/{rideId}/events/{eventId}/resolve` is called.

#### Scenario: Chaos event is active and resolvable
- **WHEN** a client calls `POST /api/rides/{rideId}/events/{eventId}/resolve` and the event is active
- **THEN** the system raises the appropriate external event (`MascotCleared`, `WeatherCleared`, or `SafetyOverride`) into the workflow and returns HTTP 202 Accepted

#### Scenario: Event not found or already resolved
- **WHEN** a client calls `POST /api/rides/{rideId}/events/{eventId}/resolve` for a non-existent or already resolved event
- **THEN** the system returns HTTP 404

### Requirement: Get ride history
The API SHALL return the last 20 completed session summaries for a ride when `GET /api/rides/{rideId}/history` is called.

#### Scenario: Ride has completed sessions
- **WHEN** a client calls `GET /api/rides/{rideId}/history` for a ride with completed sessions
- **THEN** the system returns HTTP 200 with a JSON array of up to 20 session summaries, ordered most recent first, each containing `sessionId`, `startedAt`, `completedAt`, and `outcome`

#### Scenario: Ride has no history
- **WHEN** a client calls `GET /api/rides/{rideId}/history` and no sessions have completed
- **THEN** the system returns HTTP 200 with an empty JSON array

#### Scenario: Ride not found
- **WHEN** a client calls `GET /api/rides/{rideId}/history` for a non-existent rideId
- **THEN** the system returns HTTP 404
