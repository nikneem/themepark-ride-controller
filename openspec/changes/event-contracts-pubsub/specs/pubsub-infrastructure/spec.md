## ADDED Requirements

### Requirement: Dapr pub/sub component registration
The system SHALL register a Dapr pub/sub component named `themepark-pubsub` in the Aspire AppHost, backed by Redis, so that all services can publish and subscribe using a consistent component name.

#### Scenario: Component available at startup
- **WHEN** the Aspire AppHost starts
- **THEN** a Dapr pub/sub component named `themepark-pubsub` is available and connected to the Redis instance provisioned by Aspire

#### Scenario: Services reference component by name
- **WHEN** any service publishes or subscribes to a topic
- **THEN** the Dapr component name `themepark-pubsub` is used in all `DaprClient.PublishEventAsync` calls and `[Topic]` attribute declarations

---

### Requirement: Shared event contracts library
The system SHALL provide a shared C# class library (`ThemePark.EventContracts`) containing all event record types so that publishers and subscribers use identical payload definitions.

#### Scenario: Publisher uses shared record type
- **WHEN** a service publishes an event
- **THEN** it constructs an instance of the corresponding record from `ThemePark.EventContracts` and passes it to `DaprClient.PublishEventAsync`

#### Scenario: Subscriber deserializes using shared record type
- **WHEN** Control Center API receives a Dapr pub/sub message on a subscribed topic
- **THEN** the message body is deserialized into the corresponding record type from `ThemePark.EventContracts`

---

### Requirement: camelCase JSON serialisation
All event payloads SHALL be serialised and deserialized using `System.Text.Json` with `JsonNamingPolicy.CamelCase` so that property names on the wire are camelCase regardless of the C# record property naming convention.

#### Scenario: Property names on wire are camelCase
- **WHEN** an event record is serialised to JSON
- **THEN** all property names are camelCase (e.g., `eventId`, `affectedZones`, `generatedAt`)

#### Scenario: Enum values serialised as strings
- **WHEN** an event record containing an enum field is serialised
- **THEN** the enum value is written as its string name (e.g., `"Severe"` not `2`)

---

### Requirement: Mandatory eventId for deduplication
Every event record SHALL include an `EventId` field of type `Guid` so that consumers can detect and discard duplicate deliveries.

#### Scenario: New event has unique eventId
- **WHEN** a publisher creates a new event payload
- **THEN** `EventId` is set to a freshly generated `Guid.NewGuid()`

#### Scenario: Consumer can log eventId
- **WHEN** Control Center API receives a message
- **THEN** the `eventId` value is available for logging and deduplication checks

---

### Requirement: UTC ISO 8601 timestamps
All timestamp fields in event payloads SHALL use `DateTimeOffset` serialised to UTC ISO 8601 format so that timestamps are unambiguous across services and developer machines.

#### Scenario: Timestamp serialised to UTC ISO 8601
- **WHEN** an event record with a timestamp field is serialised
- **THEN** the timestamp appears as an ISO 8601 string with UTC offset, e.g. `"2024-06-01T10:00:00Z"`

---

### Requirement: Dead letter topics for failed processing
Each Dapr subscription SHALL declare a dead letter topic named `{topic}.deadletter` so that messages that fail processing after exhausting retries are routed there for observability.

#### Scenario: Failed message routed to dead letter topic
- **WHEN** a subscriber returns a non-success HTTP status code after all Dapr retry attempts
- **THEN** Dapr routes the message to `{topic}.deadletter` (e.g., `weather.alert.deadletter`)

#### Scenario: Dead letter topic visible in Aspire dashboard
- **WHEN** a message is sent to a dead letter topic
- **THEN** the message is observable in the Aspire/Dapr dashboard or via structured logs

---

### Requirement: Subscriber registration via minimal API endpoints
Control Center API SHALL register Dapr pub/sub subscriptions using the `[Topic]` attribute on minimal API endpoint handlers and `app.MapSubscribeHandler()`, consistent with the Minimal APIs architectural decision.

#### Scenario: Subscribe handler mapped at startup
- **WHEN** Control Center API starts
- **THEN** `app.MapSubscribeHandler()` is called and all `[Topic]`-attributed endpoints are discoverable at `GET /dapr/subscribe`

#### Scenario: Subscriber endpoint returns 200 on success
- **WHEN** a subscriber endpoint successfully processes a message
- **THEN** it returns HTTP 200 so Dapr marks the message as acknowledged

#### Scenario: Subscriber endpoint returns non-200 on failure
- **WHEN** a subscriber endpoint throws an unhandled exception
- **THEN** it returns a non-200 response so Dapr retries and eventually routes to the dead letter topic
