## ADDED Requirements

### Requirement: Get ride state
The system SHALL expose a `GET /rides/{rideId}` endpoint that returns the current operational state of the specified ride. The response SHALL include `rideId`, `name`, `operationalStatus`, `capacity`, and `currentPassengerCount`. If no ride exists for the given `rideId`, the system SHALL return 404 Not Found.

#### Scenario: Successful ride state retrieval
- **WHEN** a caller sends `GET /rides/{rideId}` for an existing ride
- **THEN** the system returns HTTP 200 with a JSON body containing `rideId`, `name`, `operationalStatus`, `capacity`, and `currentPassengerCount`

#### Scenario: Ride not found
- **WHEN** a caller sends `GET /rides/{rideId}` for a `rideId` that does not exist in the state store
- **THEN** the system returns HTTP 404 Not Found

---

### Requirement: Start ride
The system SHALL expose a `POST /rides/{rideId}/start` endpoint that transitions the ride's status from `Idle` to `Running`. If the ride is not currently `Idle`, the system SHALL return 409 Conflict.

#### Scenario: Successful ride start
- **WHEN** a caller sends `POST /rides/{rideId}/start` and the ride's current status is `Idle`
- **THEN** the system updates the ride status to `Running` in the Dapr state store and returns HTTP 200

#### Scenario: Start rejected when not Idle
- **WHEN** a caller sends `POST /rides/{rideId}/start` and the ride's current status is anything other than `Idle` (e.g., `Running`, `Paused`)
- **THEN** the system returns HTTP 409 Conflict without modifying the state store

#### Scenario: Start on non-existent ride
- **WHEN** a caller sends `POST /rides/{rideId}/start` for a `rideId` that does not exist
- **THEN** the system returns HTTP 404 Not Found

---

### Requirement: Pause ride
The system SHALL expose a `POST /rides/{rideId}/pause` endpoint that transitions the ride's status from `Running` to `Paused`. The request body SHALL include a `reason` string (e.g., `"WeatherAlert"`). If the ride is not currently `Running`, the system SHALL return 409 Conflict.

#### Scenario: Successful ride pause
- **WHEN** a caller sends `POST /rides/{rideId}/pause` with a valid `reason` and the ride's current status is `Running`
- **THEN** the system updates the ride status to `Paused` (storing the reason) in the Dapr state store and returns HTTP 200

#### Scenario: Pause rejected when not Running
- **WHEN** a caller sends `POST /rides/{rideId}/pause` and the ride's current status is not `Running` (e.g., `Idle`, `Paused`)
- **THEN** the system returns HTTP 409 Conflict without modifying the state store

#### Scenario: Pause with missing reason
- **WHEN** a caller sends `POST /rides/{rideId}/pause` with a missing or empty `reason`
- **THEN** the system returns HTTP 400 Bad Request

---

### Requirement: Resume ride
The system SHALL expose a `POST /rides/{rideId}/resume` endpoint that transitions the ride's status from `Paused` to `Running`. If the ride is not currently `Paused`, the system SHALL return 409 Conflict.

#### Scenario: Successful ride resume
- **WHEN** a caller sends `POST /rides/{rideId}/resume` and the ride's current status is `Paused`
- **THEN** the system updates the ride status to `Running` in the Dapr state store and returns HTTP 200

#### Scenario: Resume rejected when not Paused
- **WHEN** a caller sends `POST /rides/{rideId}/resume` and the ride's current status is not `Paused`
- **THEN** the system returns HTTP 409 Conflict without modifying the state store

---

### Requirement: Stop ride
The system SHALL expose a `POST /rides/{rideId}/stop` endpoint that transitions the ride's status to `Idle` regardless of its current status (except `Maintenance`). If the ride is in `Maintenance`, the system SHALL return 409 Conflict.

#### Scenario: Successful ride stop from Running
- **WHEN** a caller sends `POST /rides/{rideId}/stop` and the ride's current status is `Running`
- **THEN** the system updates the ride status to `Idle` in the Dapr state store and returns HTTP 200

#### Scenario: Successful ride stop from Paused
- **WHEN** a caller sends `POST /rides/{rideId}/stop` and the ride's current status is `Paused`
- **THEN** the system updates the ride status to `Idle` in the Dapr state store and returns HTTP 200

#### Scenario: Stop rejected when in Maintenance
- **WHEN** a caller sends `POST /rides/{rideId}/stop` and the ride's current status is `Maintenance`
- **THEN** the system returns HTTP 409 Conflict without modifying the state store

---

### Requirement: Ride state persisted in Dapr state store
The system SHALL persist all ride state changes to the Dapr state store using the key format `ride-state-{rideId}`. Reads and writes SHALL use the Dapr state store API. The persisted state SHALL include all fields returned by `GET /rides/{rideId}`.

#### Scenario: State persisted after status change
- **WHEN** any status-mutating endpoint (start, pause, resume, stop) completes successfully
- **THEN** the updated state is retrievable from the Dapr state store under key `ride-state-{rideId}`

---

### Requirement: Rides pre-seeded at startup
The system SHALL pre-seed the following 5 rides into the Dapr state store on application startup if they do not already exist: Thunder Mountain (capacity 24), Space Coaster (capacity 12), Splash Canyon (capacity 20), Haunted Mansion (capacity 16), Dragon's Lair (capacity 8). All seeded rides SHALL have initial status `Idle` and `currentPassengerCount` of 0.

#### Scenario: Fresh startup seeds all rides
- **WHEN** the application starts and no ride state exists in the state store
- **THEN** all 5 rides are written to the state store with status `Idle` and `currentPassengerCount` 0

#### Scenario: Existing rides are not overwritten on restart
- **WHEN** the application starts and ride state already exists in the state store for one or more rides
- **THEN** the existing state for those rides is preserved (not overwritten)
