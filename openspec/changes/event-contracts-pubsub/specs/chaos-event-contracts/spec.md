## ADDED Requirements

### Requirement: weather.alert event contract
The system SHALL define a `WeatherAlertEvent` record with fields `EventId` (Guid), `Severity` (enum: Mild | Severe), `AffectedZones` (string[]), and `GeneratedAt` (DateTimeOffset), published on topic `weather.alert` by Weather Service when severity is not Calm.

#### Scenario: Weather alert published on non-Calm severity
- **WHEN** Weather Service generates a weather condition with severity `Mild` or `Severe`
- **THEN** it publishes a `WeatherAlertEvent` to topic `weather.alert` on component `themepark-pubsub` with a unique `eventId`, the severity value, the list of affected zone identifiers, and the current UTC timestamp in `generatedAt`

#### Scenario: Weather alert not published for Calm severity
- **WHEN** Weather Service generates a weather condition with severity `Calm`
- **THEN** no event is published to topic `weather.alert`

#### Scenario: Control Center receives weather alert
- **WHEN** Control Center API receives a message on topic `weather.alert`
- **THEN** it deserializes the message body into a `WeatherAlertEvent` record and processes the alert (e.g., triggering ride pause workflow)

#### Scenario: Malformed weather alert routed to dead letter
- **WHEN** a message on topic `weather.alert` cannot be deserialized into `WeatherAlertEvent`
- **THEN** the subscriber returns a non-200 response and Dapr routes the message to `weather.alert.deadletter`

---

### Requirement: mascot.in-restricted-zone event contract
The system SHALL define a `MascotInRestrictedZoneEvent` record with fields `EventId` (Guid), `MascotId` (string), `MascotName` (string), `AffectedRideId` (Guid), and `DetectedAt` (DateTimeOffset), published on topic `mascot.in-restricted-zone` by Mascot Service when a mascot enters a ride zone.

#### Scenario: Mascot intrusion event published
- **WHEN** Mascot Service detects (via internal timer or `POST /mascots/simulate-intrusion`) that a mascot has entered a ride restricted zone
- **THEN** it publishes a `MascotInRestrictedZoneEvent` to topic `mascot.in-restricted-zone` on component `themepark-pubsub` with the mascot's ID and name, the ID of the affected ride, and the current UTC timestamp

#### Scenario: Control Center receives mascot intrusion event
- **WHEN** Control Center API receives a message on topic `mascot.in-restricted-zone`
- **THEN** it deserializes the message into a `MascotInRestrictedZoneEvent` record and processes the intrusion (e.g., halting the affected ride)

#### Scenario: Malformed mascot event routed to dead letter
- **WHEN** a message on topic `mascot.in-restricted-zone` cannot be deserialized
- **THEN** the subscriber returns a non-200 response and Dapr routes the message to `mascot.in-restricted-zone.deadletter`

---

### Requirement: ride.malfunction event contract
The system SHALL define a `RideMalfunctionEvent` record with fields `EventId` (Guid), `RideId` (Guid), `FaultCode` (string), `Description` (string), and `OccurredAt` (DateTimeOffset), published on topic `ride.malfunction` by Ride Service when a fault is detected.

#### Scenario: Ride malfunction event published
- **WHEN** Ride Service detects a malfunction via `POST /rides/{id}/simulate-malfunction` or internal fault simulation
- **THEN** it publishes a `RideMalfunctionEvent` to topic `ride.malfunction` on component `themepark-pubsub` with the ride's ID, a fault code, a human-readable description, and the current UTC timestamp in `occurredAt`

#### Scenario: Control Center receives ride malfunction event
- **WHEN** Control Center API receives a message on topic `ride.malfunction`
- **THEN** it deserializes the message into a `RideMalfunctionEvent` record and initiates the ride fault handling workflow

#### Scenario: Malformed ride malfunction event routed to dead letter
- **WHEN** a message on topic `ride.malfunction` cannot be deserialized
- **THEN** the subscriber returns a non-200 response and Dapr routes the message to `ride.malfunction.deadletter`

---

### Requirement: WeatherSeverity enum
The system SHALL define a `WeatherSeverity` enum with values `Calm`, `Mild`, and `Severe` in the `ThemePark.EventContracts` library so that publishers and subscribers share a consistent severity vocabulary.

#### Scenario: Severity serialised as string
- **WHEN** a `WeatherAlertEvent` containing `Severity = WeatherSeverity.Severe` is serialised
- **THEN** the JSON field reads `"severity": "Severe"`

#### Scenario: Severity deserialized from string
- **WHEN** a JSON message containing `"severity": "Mild"` is deserialized into `WeatherAlertEvent`
- **THEN** `Severity` equals `WeatherSeverity.Mild`
