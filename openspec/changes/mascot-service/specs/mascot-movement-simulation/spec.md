## ADDED Requirements

### Requirement: Periodic random mascot movement
The system SHALL run an internal timer that fires every 45 seconds (configurable via `MascotSimulation:IntervalSeconds`). On each tick, every mascot SHALL be moved to a randomly selected zone. If the target zone is already occupied by another mascot, that mascot SHALL be skipped for that tick.

#### Scenario: Timer moves mascots on each tick
- **WHEN** the movement timer fires
- **THEN** each mascot's `currentZone` is updated to a randomly selected zone from the set {Park-Central, Zone-A, Zone-B, Zone-C, Backstage}

#### Scenario: Target zone already occupied
- **WHEN** the movement timer fires and Zone-B already contains mascot-002
- **THEN** any other mascot that would be assigned to Zone-B is left in its current zone for that tick

#### Scenario: Timer interval is configurable
- **WHEN** `MascotSimulation:IntervalSeconds` is set to `10`
- **THEN** the timer fires every 10 seconds instead of the default 45 seconds

---

### Requirement: Publish event when mascot enters ride zone
The system SHALL publish a `mascot.in-restricted-zone` Dapr pub/sub event each time a mascot's zone changes to Zone-A, Zone-B, or Zone-C as a result of a timer tick. The event payload SHALL include `eventId`, `mascotId`, `mascotName`, `affectedRideId`, and `detectedAt`.

#### Scenario: Mascot moves into a ride zone
- **WHEN** the movement timer moves mascot-003 from `Park-Central` to `Zone-C`
- **THEN** a `mascot.in-restricted-zone` event is published with `mascotId: "mascot-003"`, `mascotName: "Ziggy the Zebra 🦓"`, `affectedRideId` corresponding to Zone-C, and a `detectedAt` timestamp

#### Scenario: Mascot stays in a safe zone
- **WHEN** the movement timer moves mascot-001 from `Backstage` to `Park-Central`
- **THEN** no `mascot.in-restricted-zone` event is published

#### Scenario: Mascot moves between two safe zones
- **WHEN** the movement timer moves a mascot from `Zone-A` to `Park-Central`
- **THEN** no `mascot.in-restricted-zone` event is published for the departure

---

### Requirement: Service starts with all mascots in safe zone
The system SHALL initialise all mascots to `Park-Central` when the service starts.

#### Scenario: Service cold start
- **WHEN** the mascot service starts
- **THEN** mascot-001, mascot-002, and mascot-003 all have `currentZone: "Park-Central"` and `isInRestrictedZone: false`
