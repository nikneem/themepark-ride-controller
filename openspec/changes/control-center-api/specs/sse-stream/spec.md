## ADDED Requirements

### Requirement: SSE stream endpoint
The system SHALL expose `GET /api/events/stream` as a Server-Sent Events endpoint that pushes real-time events to connected frontend clients.

#### Scenario: Client connects to SSE stream
- **WHEN** a client sends `GET /api/events/stream` with `Accept: text/event-stream`
- **THEN** the server returns HTTP 200 with `Content-Type: text/event-stream`, keeps the connection open, and begins sending events as they occur

#### Scenario: Client disconnects
- **WHEN** a connected SSE client closes the connection
- **THEN** the server detects the disconnection via `HttpContext.RequestAborted`, removes the client's channel from `SseConnectionManager`, and releases all associated resources

### Requirement: ride-status-changed event
The system SHALL push a `ride-status-changed` SSE event whenever a ride's status changes (e.g., Idle → Running, Running → Paused).

#### Scenario: Ride transitions to a new status
- **WHEN** a ride session status changes
- **THEN** all connected SSE clients receive an event with `event: ride-status-changed` and a JSON data payload containing `rideId`, `previousStatus`, and `newStatus`

### Requirement: chaos-event-received event
The system SHALL push a `chaos-event-received` SSE event whenever a new chaos event affects a ride.

#### Scenario: Chaos event injected into workflow
- **WHEN** a pub/sub subscriber raises a chaos external event into a workflow
- **THEN** all connected SSE clients receive an event with `event: chaos-event-received` and a JSON data payload containing `rideId`, `eventId`, `eventType`, and `receivedAt`

### Requirement: chaos-event-resolved event
The system SHALL push a `chaos-event-resolved` SSE event whenever an operator resolves a chaos event.

#### Scenario: Operator resolves a chaos event
- **WHEN** `POST /api/rides/{rideId}/events/{eventId}/resolve` is called and succeeds
- **THEN** all connected SSE clients receive an event with `event: chaos-event-resolved` and a JSON data payload containing `rideId`, `eventId`, and `resolvedAt`

### Requirement: ride-completed event
The system SHALL push a `ride-completed` SSE event whenever a ride session reaches a terminal state.

#### Scenario: Ride session completes successfully
- **WHEN** a `RideWorkflow` instance terminates with `Completed` outcome
- **THEN** all connected SSE clients receive an event with `event: ride-completed` and a JSON data payload containing `rideId`, `sessionId`, `outcome`, and `completedAt`

#### Scenario: Ride session aborts
- **WHEN** a `RideWorkflow` instance terminates with any `Aborted*` outcome
- **THEN** all connected SSE clients receive an event with `event: ride-completed` and a JSON data payload where `outcome` reflects the abort reason

### Requirement: Per-connection Channel<T> isolation
The system SHALL use a dedicated `Channel<SseEvent>` per connected SSE client, managed by a singleton `SseConnectionManager`.

#### Scenario: Multiple clients connected simultaneously
- **WHEN** two or more SSE clients are connected
- **THEN** each client receives all events independently; a slow or disconnected client does not block event delivery to other clients

### Requirement: SSE heartbeat
The system SHALL send a periodic comment heartbeat (`: heartbeat`) to all connected SSE clients to keep connections alive through proxies and load balancers.

#### Scenario: Client is idle (no events for 15 seconds)
- **WHEN** no events have been pushed to an SSE client for 15 seconds
- **THEN** the server sends a `: heartbeat` comment line to keep the connection open
