## ADDED Requirements

### Requirement: Ride entity has defined fields
The system SHALL represent every physical attraction as a `Ride` entity with the following fields: `rideId` (GUID, stable, immutable), `name` (string), `capacity` (int, maximum simultaneous passengers), `zone` (constrained string, one of Zone-A / Zone-B / Zone-C), and `status` (RideStatus enum).

#### Scenario: Ride entity contains all mandatory fields
- **WHEN** a Ride object is constructed
- **THEN** it exposes `rideId`, `name`, `capacity`, `zone`, and `status` as non-nullable fields

#### Scenario: Zone value is constrained
- **WHEN** a Ride is constructed with a zone value outside Zone-A, Zone-B, or Zone-C
- **THEN** construction fails with an `ArgumentException` indicating an invalid zone

#### Scenario: RideId is immutable after construction
- **WHEN** a Ride entity is created with a given `rideId`
- **THEN** the `rideId` cannot be changed for the lifetime of that entity

---

### Requirement: Passenger record is immutable
The system SHALL represent every guest loaded onto a ride as a `Passenger` record with `passengerId` (GUID), `name` (string), and `isVip` (bool). All fields MUST be set at construction time and MUST NOT be mutable after creation.

#### Scenario: Passenger holds all required fields
- **WHEN** a Passenger record is created
- **THEN** it exposes `passengerId`, `name`, and `isVip` and none of these fields can be modified after construction

#### Scenario: VIP flag is set at boarding
- **WHEN** a passenger is loaded onto a ride session with `isVip = true`
- **THEN** the passenger's `isVip` remains `true` for the duration of the session and cannot be changed

---

### Requirement: At most one active workflow per ride
The system SHALL enforce that at any point in time, no ride has more than one active Dapr Workflow instance. Attempting to start a second workflow for a ride that already has one active MUST be rejected.

#### Scenario: Starting a workflow on an idle ride succeeds
- **WHEN** a ride has no active workflow and an operator requests a ride start
- **THEN** a new workflow instance is created and the ride transitions to a non-Idle status

#### Scenario: Starting a workflow on an already-active ride is rejected
- **WHEN** a ride already has an active workflow instance
- **THEN** any attempt to start another workflow for that same ride is rejected with an appropriate error (e.g. HTTP 409 Conflict)

---

### Requirement: All 5 rides are pre-seeded with stable GUIDs on service startup
The Rides Service SHALL seed exactly the following 5 rides on startup if they do not already exist. GUIDs and names are canonical and MUST NOT be changed:

| Name | Zone | Capacity | GUID |
|---|---|---|---|
| Thunder Mountain | Zone-A | 24 | `a1b2c3d4-0001-0000-0000-000000000001` |
| Space Coaster | Zone-A | 12 | `a1b2c3d4-0002-0000-0000-000000000002` |
| Splash Canyon | Zone-B | 20 | `a1b2c3d4-0003-0000-0000-000000000003` |
| Haunted Mansion | Zone-C | 16 | `a1b2c3d4-0004-0000-0000-000000000004` |
| Dragon's Lair | Zone-A | 8 | `a1b2c3d4-0005-0000-0000-000000000005` |

#### Scenario: Service boots with no existing rides
- **WHEN** the Rides Service starts and the state store contains no ride records
- **THEN** all 5 canonical rides are created with the exact GUIDs, names, zones, and capacities specified above

#### Scenario: Service boots with rides already seeded
- **WHEN** the Rides Service starts and the state store already contains the 5 canonical rides
- **THEN** no duplicate records are created and existing data is not overwritten

---

### Requirement: Zone values are constrained to Zone-A, Zone-B, Zone-C
The system SHALL accept only the values `Zone-A`, `Zone-B`, and `Zone-C` as valid zone identifiers. Any service receiving or producing a zone value MUST validate against this set.

#### Scenario: Valid zone passes validation
- **WHEN** a zone value of `Zone-A`, `Zone-B`, or `Zone-C` is provided
- **THEN** the value is accepted without error

#### Scenario: Invalid zone fails validation
- **WHEN** a zone value such as `Zone-D`, `zone-a`, or an empty string is provided
- **THEN** the system rejects the value with a validation error before persisting or processing it

---

### Requirement: WorkflowId follows the canonical naming convention
The system SHALL generate workflow instance IDs using the format `ride-{rideId}-{yyyyMMddHHmmss}`, where `rideId` is the full GUID of the ride and the timestamp is UTC.

#### Scenario: WorkflowId is generated correctly
- **WHEN** a ride workflow is started for ride `a1b2c3d4-0001-0000-0000-000000000001` at UTC 2025-01-15 14:30:22
- **THEN** the workflow instance ID is `ride-a1b2c3d4-0001-0000-0000-000000000001-20250115143022`

#### Scenario: WorkflowIds for the same ride on different days do not collide
- **WHEN** a ride workflow is started for the same rideId on two different dates
- **THEN** each generates a unique workflowId due to the differing timestamps
