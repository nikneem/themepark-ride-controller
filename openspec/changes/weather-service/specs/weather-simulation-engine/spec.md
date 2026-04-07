## ADDED Requirements

### Requirement: Timer fires on configured interval
The simulation engine SHALL start a periodic timer on application startup. The interval SHALL be read from `Weather:SimulationIntervalSeconds` (default: 60). On each tick the engine SHALL generate a new weather condition and store it as the current condition.

#### Scenario: Timer fires after configured interval
- **WHEN** the application has been running for `Weather:SimulationIntervalSeconds` seconds
- **THEN** a new weather condition is generated and stored as the current condition

#### Scenario: Default interval is 60 seconds
- **WHEN** `Weather:SimulationIntervalSeconds` is not configured
- **THEN** the timer fires every 60 seconds

---

### Requirement: Weather condition is generated with configurable probability weights
On each timer tick the engine SHALL randomly select a severity (Calm, Mild, Severe) according to weights read from configuration keys `Weather:CalmWeight`, `Weather:MildWeight`, `Weather:SevereWeight`. Default weights SHALL be 60, 30, 10 respectively. The weights are treated as relative and do not need to sum to 100.

#### Scenario: Default weights produce Calm most often
- **WHEN** 1000 timer ticks are simulated with default weights
- **THEN** approximately 60% of outcomes are Calm, 30% Mild, and 10% Severe (within statistical tolerance)

#### Scenario: Custom weights are respected
- **WHEN** `Weather:CalmWeight=0`, `Weather:MildWeight=0`, `Weather:SevereWeight=1`
- **THEN** every generated condition has severity Severe

---

### Requirement: Affected zones are randomly assigned per condition
For Mild and Severe conditions the engine SHALL randomly select one or more zones from the configured zone list (`Weather:Zones`, default: Zone-A, Zone-B, Zone-C) to include in `affectedZones`. For Calm conditions `affectedZones` SHALL be empty.

#### Scenario: Calm condition has no affected zones
- **WHEN** the generated severity is Calm
- **THEN** `affectedZones` is an empty collection

#### Scenario: Mild or Severe condition has at least one affected zone
- **WHEN** the generated severity is Mild or Severe
- **THEN** `affectedZones` contains at least one zone from the configured zone list

---

### Requirement: Calm condition does not publish a pub/sub event
When the generated severity is Calm the engine SHALL update the in-memory current condition but SHALL NOT publish any Dapr pub/sub event.

#### Scenario: No event on Calm
- **WHEN** the timer fires and generates a Calm condition
- **THEN** no message is published to the `weather.alert` topic

---

### Requirement: Mild or Severe condition publishes a weather.alert event
When the generated severity is Mild or Severe the engine SHALL publish a `weather.alert` event via Dapr pub/sub. The payload SHALL contain: `eventId` (new GUID), `severity`, `affectedZones`, `generatedAt` (UTC timestamp).

#### Scenario: Mild condition publishes alert
- **WHEN** the timer fires and generates a Mild condition
- **THEN** a message is published to topic `weather.alert` with `severity: "Mild"`, a non-empty `affectedZones`, a valid `eventId`, and a `generatedAt` timestamp

#### Scenario: Severe condition publishes alert
- **WHEN** the timer fires and generates a Severe condition
- **THEN** a message is published to topic `weather.alert` with `severity: "Severe"`, a non-empty `affectedZones`, a valid `eventId`, and a `generatedAt` timestamp

#### Scenario: Published payload contains all required fields
- **WHEN** a weather alert is published
- **THEN** the payload contains non-null/non-empty `eventId`, `severity`, `affectedZones`, and `generatedAt`

---

### Requirement: Initial condition is Calm
On startup, before the first timer tick, the current condition SHALL default to Calm with empty `affectedZones`.

#### Scenario: Service starts in Calm state
- **WHEN** the service has just started and no timer tick has fired yet
- **THEN** the current condition is Calm with empty `affectedZones`
