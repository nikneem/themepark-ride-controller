## ADDED Requirements

### Requirement: List all mascots
The system SHALL expose a `GET /mascots` endpoint that returns the current state of all mascots. Each entry SHALL include `mascotId`, `name`, `currentZone`, `isInRestrictedZone`, and `affectedRideId` (null when the mascot is not in a ride zone).

#### Scenario: All mascots in safe zones
- **WHEN** `GET /mascots` is requested and no mascot is in a ride zone
- **THEN** the response is HTTP 200 with a JSON array of all three mascots, each with `isInRestrictedZone: false` and `affectedRideId: null`

#### Scenario: One mascot in a restricted zone
- **WHEN** `GET /mascots` is requested and mascot-001 is currently in Zone-A
- **THEN** the response includes mascot-001 with `isInRestrictedZone: true` and `affectedRideId` set to the ride identifier for Zone-A

#### Scenario: All three mascots returned
- **WHEN** `GET /mascots` is requested
- **THEN** the response array contains exactly three entries: mascot-001, mascot-002, and mascot-003

---

### Requirement: Clear a mascot from a restricted zone
The system SHALL expose a `POST /mascots/{mascotId}/clear` endpoint. When called, the mascot SHALL be moved to `Park-Central`, and the service SHALL publish a `mascot.cleared` event containing `mascotId`, `clearedFromRideId`, and `clearedAt`. The response SHALL be HTTP 200 with `{ mascotId, clearedFromRideId, clearedAt }`.

#### Scenario: Successful clear
- **WHEN** `POST /mascots/mascot-001/clear` is called and mascot-001 is currently in Zone-A
- **THEN** the response is HTTP 200 with `mascotId: "mascot-001"`, `clearedFromRideId` set to Zone-A's ride ID, and a `clearedAt` timestamp
- **AND** mascot-001's `currentZone` is updated to `Park-Central`
- **AND** a `mascot.cleared` event is published

#### Scenario: Clear a mascot that is not in a restricted zone
- **WHEN** `POST /mascots/mascot-002/clear` is called and mascot-002 is currently in `Backstage`
- **THEN** the response is HTTP 404

#### Scenario: Clear with unknown mascot ID
- **WHEN** `POST /mascots/unknown-id/clear` is called
- **THEN** the response is HTTP 404

---

### Requirement: Restricted zone detection
The system SHALL classify Zone-A, Zone-B, and Zone-C as restricted (ride) zones. Park-Central and Backstage SHALL be classified as safe zones. A mascot is considered to be in a restricted zone if and only if its `currentZone` is Zone-A, Zone-B, or Zone-C.

#### Scenario: Mascot in Zone-A is restricted
- **WHEN** a mascot's `currentZone` is `Zone-A`
- **THEN** `isInRestrictedZone` is `true` and `affectedRideId` is non-null

#### Scenario: Mascot in Backstage is not restricted
- **WHEN** a mascot's `currentZone` is `Backstage`
- **THEN** `isInRestrictedZone` is `false` and `affectedRideId` is `null`
