## ADDED Requirements

### Requirement: OTel registration via ServiceDefaults
Every service SHALL register OpenTelemetry traces, metrics, and logs by calling `AddServiceDefaults()` on its `IHostApplicationBuilder`. The OTel SDK MUST be configured with the OTLP exporter pointing at the collector provided by the Aspire dashboard (local) or Azure Monitor (production).

#### Scenario: Service starts and OTel is active
- **WHEN** a service starts with `AddServiceDefaults()` called
- **THEN** the OTel SDK is registered, the OTLP exporter is configured, and traces, metrics, and logs flow to the configured collector

#### Scenario: Missing OTLP endpoint
- **WHEN** no OTLP endpoint is configured (e.g., local dev without Aspire dashboard)
- **THEN** the service starts without error; telemetry is silently dropped (no exception thrown)

---

### Requirement: HTTP request tracing
Every inbound HTTP request to any service API SHALL produce an OTel trace span. Every outbound Dapr service-invocation call SHALL propagate the trace context so spans appear as children of the calling span.

#### Scenario: Inbound HTTP request traced
- **WHEN** an HTTP request arrives at any service endpoint
- **THEN** an OTel span is created with `http.method`, `http.route`, and `http.status_code` attributes

#### Scenario: Dapr service invocation trace propagation
- **WHEN** a workflow activity calls another service via Dapr service invocation
- **THEN** the W3C `traceparent` header is forwarded, and the callee's span appears as a child of the caller's span in the trace view

#### Scenario: Dapr pub/sub publish traced
- **WHEN** a service publishes a message to a Dapr pub/sub topic
- **THEN** an OTel span is created for the publish operation and trace context is embedded in the message metadata

#### Scenario: Dapr pub/sub receive traced
- **WHEN** a service receives a message from a Dapr pub/sub topic
- **THEN** an OTel span is created for the receive/handler operation as a child of the publisher's span

---

### Requirement: Workflow step spans
Each Dapr Workflow activity execution SHALL produce an OTel child span named after the activity. The span SHALL include a `workflowId` attribute and a `step` attribute identifying the activity.

#### Scenario: Workflow activity span created
- **WHEN** a Dapr Workflow activity executes
- **THEN** a child span is created under the parent workflow span with attributes `workflowId` and `step` set to the activity name

---

### Requirement: Custom metrics
The system SHALL expose the following OTel metric instruments. All counters are monotonically increasing. The `active_workflows` instrument is an `ObservableGauge`.

| Instrument name | Type | Labels |
|---|---|---|
| `rides_started_total` | Counter | — |
| `rides_completed_total` | Counter | — |
| `rides_failed_total` | Counter | — |
| `chaos_events_total` | Counter | `type` |
| `refunds_issued_total` | Counter | — |
| `active_workflows` | ObservableGauge | — |

Instruments SHALL be defined on a `Meter` named `"ThemePark.<ServiceName>"`. `ServiceDefaults` SHALL register `AddMeter("ThemePark.*")` so all theme-park meters are captured by the OTel pipeline.

#### Scenario: Ride started increments counter
- **WHEN** a ride workflow is started
- **THEN** `rides_started_total` is incremented by 1

#### Scenario: Ride completed increments counter
- **WHEN** a ride workflow completes successfully
- **THEN** `rides_completed_total` is incremented by 1

#### Scenario: Ride failed increments counter
- **WHEN** a ride workflow terminates with a failure
- **THEN** `rides_failed_total` is incremented by 1

#### Scenario: Chaos event increments labelled counter
- **WHEN** a chaos event of type `malfunction` fires
- **THEN** `chaos_events_total{type="malfunction"}` is incremented by 1

#### Scenario: Refund issued increments counter
- **WHEN** a refund is processed
- **THEN** `refunds_issued_total` is incremented by 1

#### Scenario: Active workflows gauge reflects current count
- **WHEN** the OTel pipeline scrapes `active_workflows`
- **THEN** the value equals the number of in-progress Dapr Workflow instances at that moment

---

### Requirement: Structured logs with scope fields
All log statements emitted by workflow activities and ride-lifecycle handlers SHALL include the fields `rideId`, `workflowId`, and `step` via `ILogger.BeginScope`. Logs SHALL be emitted as structured JSON.

#### Scenario: Log scope includes rideId and workflowId
- **WHEN** a workflow activity logs any message
- **THEN** the structured log entry contains `rideId` and `workflowId` fields matching the current execution context

#### Scenario: Log scope includes step
- **WHEN** a workflow activity logs any message
- **THEN** the structured log entry contains a `step` field matching the activity name

#### Scenario: Logs are structured JSON
- **WHEN** a service emits a log at any level
- **THEN** the log output is valid structured JSON (not plain text) when the JSON formatter is active
