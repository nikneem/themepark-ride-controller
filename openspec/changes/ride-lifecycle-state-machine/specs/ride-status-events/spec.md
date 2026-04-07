## ADDED Requirements

### Requirement: ride.status-changed event published on every state transition
The Control Center SHALL publish a `ride.status-changed` pub/sub event on the Dapr pub/sub broker after every successful ride state transition. The event payload SHALL contain: `rideId` (string), `previousStatus` (string), `newStatus` (string), `workflowStep` (string, nullable), and `changedAt` (ISO-8601 UTC timestamp).

#### Scenario: Event published on valid transition
- **WHEN** ride `"ride-42"` transitions from `Idle` to `PreFlight`
- **THEN** a `ride.status-changed` event is published with `rideId = "ride-42"`, `previousStatus = "Idle"`, `newStatus = "PreFlight"`, and a `changedAt` timestamp

#### Scenario: Event payload contains correct shape
- **WHEN** a `ride.status-changed` event is published
- **THEN** the payload object contains exactly the fields: `rideId`, `previousStatus`, `newStatus`, `workflowStep`, and `changedAt`

#### Scenario: No event published when transition fails
- **WHEN** an invalid state transition is attempted
- **THEN** no `ride.status-changed` event is published

### Requirement: SSE stream forwards ride.status-changed to connected clients
The Control Center API SHALL expose a Server-Sent Events (SSE) endpoint at `GET /api/events/stream`. The endpoint SHALL subscribe to the internal `Channel<T>` that receives forwarded `ride.status-changed` pub/sub events and SHALL stream each event to all currently connected SSE clients.

#### Scenario: Connected client receives ride status event
- **WHEN** a client is connected to `GET /api/events/stream` and a `ride.status-changed` event arrives
- **THEN** the client receives an SSE message containing the event payload

#### Scenario: Multiple clients each receive the event
- **WHEN** two clients are connected to `GET /api/events/stream` and a `ride.status-changed` event arrives
- **THEN** both clients receive the SSE message

#### Scenario: Client receives no events before any transition occurs
- **WHEN** a client connects to `GET /api/events/stream` with no pending events
- **THEN** the connection remains open and no data is sent until the next event arrives

### Requirement: SSE clients receive all ride.status-changed events (client-side filtering)
The SSE endpoint SHALL forward every `ride.status-changed` event to all connected clients regardless of `rideId`. Client-side filtering of events by ride is the responsibility of the frontend consumer, not the server.

#### Scenario: Client receives events for all rides
- **WHEN** a client is connected to `GET /api/events/stream` and transitions occur for rides `"ride-1"` and `"ride-2"`
- **THEN** the client receives two SSE messages, one for each ride

#### Scenario: Server does not filter by ride
- **WHEN** a `ride.status-changed` event arrives for any ride
- **THEN** the server forwards it to all connected clients without inspecting `rideId`

### Requirement: SSE connection cleanup on client disconnect
When an SSE client disconnects, the Control Center SHALL stop writing to that client's response stream and SHALL release any associated resources to prevent memory or channel subscription leaks.

#### Scenario: Disconnected client is removed from active streams
- **WHEN** a connected SSE client disconnects (e.g., browser tab closed)
- **THEN** the server detects the cancellation via `CancellationToken` and stops forwarding events to that client

#### Scenario: Remaining clients continue receiving events after one disconnects
- **WHEN** one of two connected SSE clients disconnects
- **THEN** the remaining client continues to receive subsequent `ride.status-changed` events

#### Scenario: No resource leak after client disconnect
- **WHEN** a client disconnects and a subsequent `ride.status-changed` event arrives
- **THEN** the server does not attempt to write to the disconnected client's response stream
