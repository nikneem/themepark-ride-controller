## ADDED Requirements

### Requirement: Simulate malfunction endpoint
The system SHALL expose a `POST /rides/{rideId}/simulate-malfunction` endpoint that publishes a `ride.malfunction` pub/sub event via Dapr. This endpoint SHALL only be available when the `Dapr:DemoMode` configuration flag is `true`. If the flag is `false` or absent, the endpoint SHALL return 404 Not Found.

#### Scenario: Malfunction simulated successfully in demo mode
- **WHEN** `Dapr:DemoMode` is `true` and a caller sends `POST /rides/{rideId}/simulate-malfunction` for an existing ride
- **THEN** the system publishes a `ride.malfunction` event to the Dapr pub/sub broker and returns HTTP 200

#### Scenario: Endpoint returns 404 when demo mode is disabled
- **WHEN** `Dapr:DemoMode` is `false` (or not configured) and a caller sends `POST /rides/{rideId}/simulate-malfunction`
- **THEN** the system returns HTTP 404 Not Found without publishing any event

#### Scenario: Malfunction on non-existent ride
- **WHEN** `Dapr:DemoMode` is `true` and a caller sends `POST /rides/{rideId}/simulate-malfunction` for a `rideId` that does not exist
- **THEN** the system returns HTTP 404 Not Found without publishing any event

---

### Requirement: Malfunction event payload
The `ride.malfunction` pub/sub event SHALL include the `rideId`, `rideName`, and a `malfunctionTimestamp` (UTC ISO-8601). The event SHALL be published to the Dapr pub/sub component using the topic name `ride.malfunction`.

#### Scenario: Event payload contains required fields
- **WHEN** the simulate-malfunction endpoint successfully publishes an event
- **THEN** the published message contains `rideId`, `rideName`, and `malfunctionTimestamp` in the payload

#### Scenario: Timestamp is UTC
- **WHEN** the simulate-malfunction endpoint publishes the event
- **THEN** the `malfunctionTimestamp` field is a valid UTC ISO-8601 datetime string
