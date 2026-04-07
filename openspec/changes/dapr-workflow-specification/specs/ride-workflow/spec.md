## ADDED Requirements

### Requirement: Workflow is scheduled via the start-ride endpoint
The system SHALL expose a `POST /api/rides/{rideId}/start` endpoint that schedules a new `RideWorkflow` instance with a unique workflow ID in the format `ride-{rideId}-{yyyyMMddHHmmss}` and returns HTTP 202 Accepted with the workflow ID.

#### Scenario: Start ride successfully
- **WHEN** a POST request is made to `/api/rides/{rideId}/start`
- **THEN** a new workflow instance is scheduled with ID `ride-{rideId}-{timestamp}` and HTTP 202 is returned containing the workflow ID

#### Scenario: Duplicate start is handled by pre-flight check
- **WHEN** a POST request is made to `/api/rides/{rideId}/start` while the ride is already running
- **THEN** the workflow is scheduled but fails immediately when `CheckRideStatusActivity` finds the ride status is not Idle

---

### Requirement: Pre-flight — ride status must be Idle
The system SHALL execute `CheckRideStatusActivity` as the first workflow step. This activity SHALL call `GET /rides/{rideId}` via Dapr service invocation to the `ride-service`. The workflow SHALL fail immediately if the ride status is not `Idle`.

#### Scenario: Ride is Idle — pre-flight passes
- **WHEN** `CheckRideStatusActivity` calls `GET /rides/{rideId}` and the response status is `Idle`
- **THEN** the activity completes successfully and the workflow proceeds to the parallel pre-flight checks

#### Scenario: Ride is not Idle — workflow fails
- **WHEN** `CheckRideStatusActivity` calls `GET /rides/{rideId}` and the response status is not `Idle`
- **THEN** the activity throws, the workflow enters the failure path, and the final workflow state is `Failed`

---

### Requirement: Pre-flight — weather and mascot checks run in parallel
The system SHALL execute `CheckWeatherActivity` and `CheckMascotActivity` concurrently using fan-out. Both activities MUST complete successfully for the workflow to proceed. If either fails, the workflow SHALL enter the failure path.

#### Scenario: Both checks pass
- **WHEN** `CheckWeatherActivity` returns a non-Severe weather condition AND `CheckMascotActivity` finds no mascot in the ride zone
- **THEN** both tasks complete and the workflow proceeds to `LoadPassengersActivity`

#### Scenario: Weather is Severe — fan-out fails
- **WHEN** `CheckWeatherActivity` detects Severe weather
- **THEN** the activity throws, the fan-out fails, and the workflow enters the failure path

#### Scenario: Mascot in zone — fan-out fails
- **WHEN** `CheckMascotActivity` finds a mascot in the ride zone
- **THEN** the activity throws, the fan-out fails, and the workflow enters the failure path

---

### Requirement: Passengers are loaded before ride starts
The system SHALL execute `LoadPassengersActivity` after the parallel pre-flight checks pass. This activity SHALL call `POST /queue/{rideId}/load` via Dapr service invocation to the `queue-service`. The activity response SHALL include a VIP flag that is recorded in workflow state for use in downstream steps.

#### Scenario: Passengers loaded successfully
- **WHEN** `LoadPassengersActivity` calls `POST /queue/{rideId}/load` and receives a successful response
- **THEN** the activity records the VIP flag from the response and the workflow proceeds to `StartRideActivity`

---

### Requirement: Ride is started after passengers are loaded
The system SHALL execute `StartRideActivity` after `LoadPassengersActivity` completes. This activity SHALL call `POST /rides/{rideId}/start` via Dapr service invocation to the `ride-service`.

#### Scenario: Ride started successfully
- **WHEN** `StartRideActivity` calls `POST /rides/{rideId}/start` and receives a successful response
- **THEN** the activity completes and the workflow enters the running loop

---

### Requirement: Running loop waits for completion or external events
The system SHALL enter a running loop after `StartRideActivity` completes. The loop SHALL simultaneously wait for a ride completion timer and any configured external events. The first event to arrive determines the next workflow step.

#### Scenario: Ride completes normally via timer
- **WHEN** the ride completion timer (default 90 seconds) elapses with no external events
- **THEN** the workflow exits the loop and executes `CompleteRideActivity`

---

### Requirement: Ride is completed on the happy path
The system SHALL execute `CompleteRideActivity` when the running loop exits via the completion timer. This activity SHALL call `POST /rides/{rideId}/stop` via Dapr service invocation to the `ride-service` and log the completion event. The workflow SHALL end with status `Completed`.

#### Scenario: Ride completed successfully
- **WHEN** `CompleteRideActivity` calls `POST /rides/{rideId}/stop` successfully
- **THEN** the activity logs the completion and the workflow ends with final status `Completed`

---

### Requirement: All activity calls use a shared retry policy with exponential backoff
The system SHALL apply a shared retry policy to every activity call: maximum 3 attempts, first retry interval 2 seconds, backoff coefficient 2.0, maximum retry interval 8 seconds, and a per-activity timeout of 30 seconds.

#### Scenario: Activity fails transiently and succeeds on retry
- **WHEN** an activity call fails with a transient error on the first attempt
- **THEN** the workflow retries the call after 2 seconds, then 4 seconds if needed, up to 3 total attempts before propagating the failure

#### Scenario: Activity exceeds timeout
- **WHEN** an activity call does not respond within 30 seconds
- **THEN** the call is cancelled and counted as a failed attempt subject to the retry policy
