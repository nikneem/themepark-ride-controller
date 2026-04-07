## ADDED Requirements

### Requirement: Simulate mascot intrusion endpoint
The system SHALL expose a `POST /mascots/simulate-intrusion` endpoint that immediately moves the specified mascot into the specified ride zone and publishes a `mascot.in-restricted-zone` event. The endpoint SHALL only be registered when the feature flag `Dapr:DemoMode` is `true`. The request body SHALL be `{ mascotId, targetRideId }`. On success the response SHALL be HTTP 202.

#### Scenario: Successful intrusion simulation
- **WHEN** `POST /mascots/simulate-intrusion` is called with `{ mascotId: "mascot-002", targetRideId: "ride-zone-b" }` and `Dapr:DemoMode` is `true`
- **THEN** the response is HTTP 202
- **AND** mascot-002's `currentZone` is immediately updated to `Zone-B`
- **AND** a `mascot.in-restricted-zone` event is published for mascot-002

#### Scenario: Endpoint not available when DemoMode is disabled
- **WHEN** `Dapr:DemoMode` is `false` and a request is sent to `POST /mascots/simulate-intrusion`
- **THEN** the response is HTTP 404 (endpoint not registered)

---

### Requirement: Validate mascot ID on simulate-intrusion
The system SHALL return HTTP 400 if the `mascotId` in the request body does not match any known mascot (mascot-001, mascot-002, mascot-003).

#### Scenario: Unknown mascot ID
- **WHEN** `POST /mascots/simulate-intrusion` is called with `{ mascotId: "mascot-999", targetRideId: "ride-zone-a" }`
- **THEN** the response is HTTP 400

---

### Requirement: Validate target ride ID on simulate-intrusion
The system SHALL return HTTP 400 if the `targetRideId` does not correspond to a known ride zone (Zone-A, Zone-B, Zone-C).

#### Scenario: Unknown ride ID
- **WHEN** `POST /mascots/simulate-intrusion` is called with `{ mascotId: "mascot-001", targetRideId: "ride-zone-unknown" }`
- **THEN** the response is HTTP 400

#### Scenario: Safe zone provided as target
- **WHEN** `POST /mascots/simulate-intrusion` is called with `{ mascotId: "mascot-001", targetRideId: "Park-Central" }`
- **THEN** the response is HTTP 400

---

### Requirement: simulate-intrusion bypasses the movement timer
The system SHALL apply the zone change and publish the event immediately, without waiting for the next timer tick.

#### Scenario: Immediate state update
- **WHEN** `POST /mascots/simulate-intrusion` is called
- **THEN** the mascot's `currentZone` is updated before the HTTP response is returned
- **AND** the `mascot.in-restricted-zone` event is published before the HTTP response is returned
