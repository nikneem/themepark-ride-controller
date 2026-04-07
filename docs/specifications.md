# 📋 Theme Park Ride Controller — Functional Specifications

> This document provides implementation-ready specifications for the Theme Park Ride Controller system. It is intended to drive development and is the primary reference for what each service must do, what API contracts each exposes, and how the services interact.
>
> Architecture decisions are governed by the [ADRs in the Hexmaster coding guidelines](../.vscode/mcp.json). In particular:
> - **ADR-0001**: Target .NET 10
> - **ADR-0004**: CQRS — all application logic lives in command/query handlers
> - **ADR-0005**: Minimal APIs — thin endpoints only
> - **ADR-0007**: Vertical Slice Architecture — feature folders, not technical layers

---

## Table of Contents

1. [Domain Overview](#1-domain-overview)
2. [Ride Lifecycle State Machine](#2-ride-lifecycle-state-machine)
3. [Service Specifications](#3-service-specifications)
   - [Control Center API](#31-control-center-api)
   - [Ride Service](#32-ride-service)
   - [Queue Service](#33-queue-service)
   - [Maintenance Service](#34-maintenance-service)
   - [Weather Service](#35-weather-service)
   - [Mascot Service](#36-mascot-service)
   - [Refund Service](#37-refund-service)
4. [Dapr Workflow Specification](#4-dapr-workflow-specification)
5. [Event Contracts (Pub/Sub)](#5-event-contracts-pubsub)
6. [Shared Domain Models](#6-shared-domain-models)
7. [Non-Functional Requirements](#7-non-functional-requirements)
8. [Testing Specifications](#8-testing-specifications)

---

## 1. Domain Overview

The **Theme Park Ride Controller** simulates the operation of rides in a futuristic theme park. Each ride is a long-running **Dapr Workflow** that moves through a defined lifecycle, reacting to external events (weather, mascots, mechanical failures, operator decisions).

### Core Concepts

| Concept | Description |
|---|---|
| **Ride** | A physical attraction identified by a `rideId` (GUID). Has a name, capacity, and operational status. |
| **Workflow Instance** | A running Dapr Workflow for a specific ride session. One ride has at most one active workflow at a time. |
| **Chaos Event** | An unexpected occurrence (weather, mascot intrusion, mechanical failure) that the workflow must react to. |
| **Operator Action** | A human decision injected into the workflow as an external event (approve maintenance, clear mascot, etc.). |
| **Passenger** | A guest loaded onto a ride for a session. May be a VIP. |
| **Refund** | Compensation issued to passengers when a ride session ends prematurely. |

### Rides in the Park

The system ships with five pre-seeded rides:

| Ride ID (stable GUID) | Name | Capacity |
|---|---|---|
| `a1b2c3d4-0001-0000-0000-000000000001` | Thunder Mountain | 24 |
| `a1b2c3d4-0002-0000-0000-000000000002` | Space Coaster | 12 |
| `a1b2c3d4-0003-0000-0000-000000000003` | Splash Canyon | 20 |
| `a1b2c3d4-0004-0000-0000-000000000004` | Haunted Mansion | 16 |
| `a1b2c3d4-0005-0000-0000-000000000005` | Dragon's Lair | 8 |

---

## 2. Ride Lifecycle State Machine

Each ride session moves through the following states. Only one state is active at a time.

```
          ┌─────────────────────────────────────────────────┐
          │                                                 │
  ┌───────▼──────┐    ┌──────────────┐    ┌──────────────┐ │
  │     Idle     │───▶│  PreFlight   │───▶│   Loading    │ │
  └──────────────┘    └──────────────┘    └──────┬───────┘ │
          ▲                                       │         │
          │                                ┌──────▼───────┐ │
          │                                │   Running    │ │
          │                                └──────┬───────┘ │
          │                    ┌───────────────────┤         │
          │             ┌──────▼──────┐   ┌────────▼──────┐ │
          │             │   Paused    │   │ Maintenance   │ │
          │             └──────┬──────┘   └───────┬───────┘ │
          │                    │                   │         │
          │             ┌──────▼──────┐            │         │
          │             │  Resuming   │◀───────────┘         │
          │             └──────┬──────┘                      │
          │                    │                             │
          │             ┌──────▼──────┐                      │
          └─────────────│  Completed  │                      │
                        └─────────────┘                      │
                                                             │
          ┌──────────────────────────────────────────────────┘
          │  (Failure path — triggers compensation)
   ┌──────▼──────┐
   │    Failed   │
   └─────────────┘
```

### State Transition Rules

| From | To | Trigger |
|---|---|---|
| `Idle` | `PreFlight` | `POST /rides/{id}/start` |
| `PreFlight` | `Loading` | All pre-flight checks pass |
| `PreFlight` | `Failed` | Any pre-flight check fails |
| `Loading` | `Running` | Passengers successfully loaded |
| `Running` | `Paused` | Chaos event received (weather/mascot) |
| `Running` | `Maintenance` | `ride.malfunction` event received |
| `Running` | `Completed` | Ride duration elapsed normally |
| `Paused` | `Running` | Operator resolves chaos event |
| `Maintenance` | `Resuming` | Operator approves repair + `maintenance.completed` |
| `Resuming` | `Running` | Ride resumes successfully |
| `Any` | `Failed` | Unrecoverable error; triggers compensation |
| `Failed` | `Idle` | After compensation completes |
| `Completed` | `Idle` | After unload completes |

---

## 3. Service Specifications

### 3.1 Control Center API

**Project**: `ThemePark.ControlCenter.Api`  
**Responsibility**: API gateway and Dapr Workflow orchestrator.  
**Port (local)**: `5100`

#### 3.1.1 Endpoints

All endpoints follow Minimal API style (ADR-0005). Handlers are injected per ADR-0004.

---

##### `GET /api/rides`

Returns all rides and their current operational status.

**Response `200 OK`:**
```json
[
  {
    "rideId": "a1b2c3d4-0001-0000-0000-000000000001",
    "name": "Thunder Mountain",
    "capacity": 24,
    "status": "Idle",
    "activeWorkflowId": null,
    "lastUpdated": "2026-01-15T10:30:00Z"
  }
]
```

---

##### `GET /api/rides/{rideId}/status`

Returns the full status of a specific ride, including active chaos events and workflow step.

**Response `200 OK`:**
```json
{
  "rideId": "a1b2c3d4-0001-0000-0000-000000000001",
  "name": "Thunder Mountain",
  "status": "Running",
  "workflowStep": "RunningRide",
  "passengersOnBoard": 20,
  "hasVip": true,
  "activeChaosEvents": [
    {
      "eventId": "evt-abc123",
      "type": "WeatherAlert",
      "severity": "Mild",
      "receivedAt": "2026-01-15T10:45:00Z",
      "resolved": false
    }
  ],
  "workflowStartedAt": "2026-01-15T10:30:00Z",
  "lastUpdated": "2026-01-15T10:45:00Z"
}
```

**Response `404 Not Found`** — ride not found.

---

##### `POST /api/rides/{rideId}/start`

Starts a new ride session. Creates a Dapr Workflow instance.

**Preconditions:**
- Ride must be in `Idle` state.
- No active workflow instance for this ride.

**Response `202 Accepted`:**
```json
{
  "workflowId": "workflow-thunder-20260115-103000"
}
```

**Response `409 Conflict`** — ride already has an active session.  
**Response `404 Not Found`** — ride not found.

---

##### `POST /api/rides/{rideId}/maintenance/approve`

Operator approves a pending maintenance request. Raises `MaintenanceApproved` external event into the running workflow.

**Preconditions:**
- Ride must be in `Maintenance` state.

**Response `202 Accepted`**  
**Response `409 Conflict`** — ride is not awaiting maintenance approval.  
**Response `404 Not Found`** — ride not found.

---

##### `POST /api/rides/{rideId}/events/{eventId}/resolve`

Operator resolves a chaos event (e.g., clears a mascot, acknowledges weather). Raises a `ChaosEventResolved` external event into the workflow.

**Request body:**
```json
{
  "resolution": "MascotCleared"
}
```

**Allowed resolution values:** `MascotCleared`, `WeatherCleared`, `SafetyOverride`

**Response `202 Accepted`**  
**Response `404 Not Found`** — ride or event not found.  
**Response `409 Conflict`** — event already resolved.

---

##### `GET /api/rides/{rideId}/history`

Returns the completed workflow sessions for a ride (last 20).

**Response `200 OK`:**
```json
[
  {
    "workflowId": "workflow-thunder-20260115-103000",
    "startedAt": "2026-01-15T10:30:00Z",
    "completedAt": "2026-01-15T10:52:00Z",
    "outcome": "Completed",
    "passengersServed": 20,
    "chaosEventsHandled": 2,
    "refundsIssued": 0
  }
]
```

---

##### `GET /api/events/stream` (Server-Sent Events)

Real-time SSE stream pushing ride status changes and chaos events to connected frontends.

**Event types pushed:**

| Event Name | Payload |
|---|---|
| `ride-status-changed` | `{ rideId, newStatus, workflowStep }` |
| `chaos-event-received` | `{ rideId, eventId, type, severity }` |
| `chaos-event-resolved` | `{ rideId, eventId }` |
| `ride-completed` | `{ rideId, outcome, passengersServed }` |

---

#### 3.1.2 Vertical Slice Structure

```
ThemePark.ControlCenter/
  Rides/
    GetAllRides/
      GetAllRidesQuery.cs
      GetAllRidesHandler.cs
      RideSummaryDto.cs
    GetRideStatus/
      GetRideStatusQuery.cs
      GetRideStatusHandler.cs
      RideStatusDto.cs
      ChaosEventDto.cs
    StartRide/
      StartRideCommand.cs
      StartRideHandler.cs
      StartRideResult.cs
    ApproveMaintenance/
      ApproveMaintenanceCommand.cs
      ApproveMaintenanceHandler.cs
    ResolveChaosEvent/
      ResolveChaosEventCommand.cs
      ResolveChaosEventHandler.cs
    GetRideHistory/
      GetRideHistoryQuery.cs
      GetRideHistoryHandler.cs
      RideSessionHistoryDto.cs
    _Shared/
      IRideRepository.cs
      RideStatus.cs
      ChaosEventType.cs
  Workflows/
    RideWorkflow.cs
    Activities/
      CheckRideStatusActivity.cs
      CheckWeatherActivity.cs
      CheckMascotActivity.cs
      LoadPassengersActivity.cs
      StartRideActivity.cs
      PauseRideActivity.cs
      ResumeRideActivity.cs
      TriggerMaintenanceActivity.cs
      IssueRefundActivity.cs
      CompleteRideActivity.cs
  EventSubscriptions/
    WeatherAlertSubscription.cs
    MascotAlertSubscription.cs
    MaintenanceCompletedSubscription.cs
    RideMalfunctionSubscription.cs
```

---

### 3.2 Ride Service

**Project**: `ThemePark.Rides.Api`  
**Responsibility**: Controls the operational state of each physical ride.  
**Port (local)**: `5101`

#### Endpoints

All endpoints are called via Dapr service invocation (`app-id: ride-service`).

---

##### `GET /rides/{rideId}`

Returns the current operational state of a ride.

**Response `200 OK`:**
```json
{
  "rideId": "a1b2c3d4-0001-0000-0000-000000000001",
  "name": "Thunder Mountain",
  "operationalStatus": "Idle",
  "capacity": 24,
  "currentPassengerCount": 0
}
```

---

##### `POST /rides/{rideId}/start`

Puts the ride into `Running` operational status.

**Response `200 OK`:**
```json
{ "rideId": "...", "operationalStatus": "Running" }
```

**Response `409 Conflict`** — ride is not in `Idle` state.

---

##### `POST /rides/{rideId}/pause`

Pauses the ride (e.g., during a chaos event).

**Request body:**
```json
{ "reason": "WeatherAlert" }
```

**Response `200 OK`:**
```json
{ "rideId": "...", "operationalStatus": "Paused" }
```

---

##### `POST /rides/{rideId}/resume`

Resumes a paused ride.

**Response `200 OK`:**
```json
{ "rideId": "...", "operationalStatus": "Running" }
```

---

##### `POST /rides/{rideId}/stop`

Stops the ride and returns it to `Idle`.

**Response `200 OK`:**
```json
{ "rideId": "...", "operationalStatus": "Idle" }
```

---

##### `POST /rides/{rideId}/simulate-malfunction`

Development/demo endpoint. Triggers a `ride.malfunction` pub/sub event. Simulates a mechanical failure mid-ride.

**Response `202 Accepted`**

---

#### Domain Events Published

| Topic | Trigger |
|---|---|
| `ride.malfunction` | Called via `POST /rides/{id}/simulate-malfunction` or random internal fault simulation |

---

#### Vertical Slice Structure

```
ThemePark.Rides/
  GetRide/
    GetRideQuery.cs
    GetRideHandler.cs
    RideStateDto.cs
  StartRide/
    StartRideCommand.cs
    StartRideHandler.cs
  PauseRide/
    PauseRideCommand.cs
    PauseRideHandler.cs
  ResumeRide/
    ResumeRideCommand.cs
    ResumeRideHandler.cs
  StopRide/
    StopRideCommand.cs
    StopRideHandler.cs
  SimulateMalfunction/
    SimulateMalfunctionCommand.cs
    SimulateMalfunctionHandler.cs
  _Shared/
    IRideStateRepository.cs
    RideOperationalStatus.cs
    RideState.cs
```

---

### 3.3 Queue Service

**Project**: `ThemePark.Queue.Api`  
**Responsibility**: Manages passenger queues for each ride.  
**Port (local)**: `5102`

#### Endpoints

---

##### `GET /queue/{rideId}`

Returns the current queue state.

**Response `200 OK`:**
```json
{
  "rideId": "...",
  "waitingCount": 47,
  "hasVip": true,
  "estimatedWaitMinutes": 12
}
```

---

##### `POST /queue/{rideId}/load`

Loads passengers onto the ride. Returns a passenger manifest and dequeues them.

**Request body:**
```json
{ "capacity": 24 }
```

**Response `200 OK`:**
```json
{
  "passengers": [
    { "passengerId": "p-001", "name": "Alice Smith", "isVip": true },
    { "passengerId": "p-002", "name": "Bob Jones", "isVip": false }
  ],
  "loadedCount": 2,
  "vipCount": 1,
  "remainingInQueue": 45
}
```

---

##### `POST /queue/{rideId}/simulate-queue`

Development/demo endpoint. Seeds the queue with randomised passengers (including random VIPs).

**Request body:**
```json
{ "count": 50, "vipProbability": 0.1 }
```

**Response `200 OK`:**
```json
{ "queued": 50 }
```

---

### 3.4 Maintenance Service

**Project**: `ThemePark.Maintenance.Api`  
**Responsibility**: Logs and tracks repair requests.  
**Port (local)**: `5103`

#### Endpoints

---

##### `POST /maintenance/request`

Logs a new maintenance request from the workflow.

**Request body:**
```json
{
  "rideId": "...",
  "reason": "MechanicalFailure",
  "workflowId": "workflow-thunder-20260115-103000",
  "requestedAt": "2026-01-15T10:45:00Z"
}
```

**Response `201 Created`:**
```json
{
  "maintenanceId": "maint-abc123",
  "rideId": "...",
  "status": "Pending"
}
```

---

##### `POST /maintenance/{maintenanceId}/complete`

Marks a repair as done. Publishes `maintenance.completed`.

**Response `200 OK`:**
```json
{
  "maintenanceId": "maint-abc123",
  "status": "Completed",
  "completedAt": "2026-01-15T11:00:00Z"
}
```

---

##### `GET /maintenance/{rideId}/history`

Returns the last 20 maintenance records for a ride.

**Response `200 OK`:**
```json
[
  {
    "maintenanceId": "maint-abc123",
    "reason": "MechanicalFailure",
    "status": "Completed",
    "requestedAt": "2026-01-15T10:45:00Z",
    "completedAt": "2026-01-15T11:00:00Z",
    "durationMinutes": 15
  }
]
```

---

#### Domain Events Published

| Topic | Trigger |
|---|---|
| `maintenance.requested` | `POST /maintenance/request` |
| `maintenance.completed` | `POST /maintenance/{id}/complete` |

---

### 3.5 Weather Service

**Project**: `ThemePark.Weather.Api`  
**Responsibility**: Simulates weather conditions and emits alerts.  
**Port (local)**: `5104`

The Weather Service runs an **internal timer** (configurable interval, default 60 seconds) that generates weather conditions. When severity exceeds `Calm`, it publishes a `weather.alert` event.

#### Endpoints

---

##### `GET /weather/current`

Returns the current simulated weather condition.

**Response `200 OK`:**
```json
{
  "condition": "Stormy",
  "severity": "Severe",
  "affectedZones": ["Zone-A", "Zone-B"],
  "generatedAt": "2026-01-15T10:40:00Z"
}
```

---

##### `POST /weather/simulate`

Development/demo endpoint. Forces a specific weather condition immediately.

**Request body:**
```json
{
  "severity": "Severe",
  "affectedZones": ["Zone-A"]
}
```

**Allowed severity values:** `Calm`, `Mild`, `Severe`

**Response `202 Accepted`**

---

#### Domain Events Published

| Topic | Payload |
|---|---|
| `weather.alert` | `{ severity, affectedZones, generatedAt }` |

> Only `Mild` and `Severe` severities trigger an event. `Calm` is silent.

---

### 3.6 Mascot Service

**Project**: `ThemePark.Mascots.Api`  
**Responsibility**: Tracks mascot locations and raises restricted-zone alerts.  
**Port (local)**: `5105`

The Mascot Service maintains simulated positions for each park mascot and periodically moves them. When a mascot enters a ride zone, it publishes a `mascot.in-restricted-zone` event.

#### Mascots

| Mascot ID | Name |
|---|---|
| `mascot-001` | Roary the Lion 🦁 |
| `mascot-002` | Bella the Bear 🐻 |
| `mascot-003` | Ziggy the Zebra 🦓 |

#### Endpoints

---

##### `GET /mascots`

Returns current locations of all mascots.

**Response `200 OK`:**
```json
[
  {
    "mascotId": "mascot-001",
    "name": "Roary the Lion",
    "currentZone": "Zone-A",
    "isInRestrictedZone": true,
    "affectedRideId": "a1b2c3d4-0001-0000-0000-000000000001"
  }
]
```

---

##### `POST /mascots/{mascotId}/clear`

Clears a mascot from a restricted zone. Publishes `mascot.cleared`.

**Response `200 OK`:**
```json
{
  "mascotId": "mascot-001",
  "clearedFromRideId": "...",
  "clearedAt": "2026-01-15T10:50:00Z"
}
```

---

##### `POST /mascots/simulate-intrusion`

Development/demo endpoint. Forces a mascot into a ride zone.

**Request body:**
```json
{
  "mascotId": "mascot-001",
  "targetRideId": "a1b2c3d4-0001-0000-0000-000000000001"
}
```

**Response `202 Accepted`**

---

#### Domain Events Published

| Topic | Payload |
|---|---|
| `mascot.in-restricted-zone` | `{ mascotId, mascotName, affectedRideId, detectedAt }` |

---

### 3.7 Refund Service

**Project**: `ThemePark.Refunds.Api`  
**Responsibility**: Issues passenger refunds and compensation vouchers.  
**Port (local)**: `5106`

The Refund Service is called as a **compensation activity** by the ride workflow when a session ends in failure.

#### Endpoints

---

##### `POST /refunds`

Issues refunds to a list of passengers.

**Request body:**
```json
{
  "rideId": "...",
  "workflowId": "...",
  "reason": "MechanicalFailure",
  "passengers": [
    { "passengerId": "p-001", "name": "Alice Smith", "isVip": true }
  ]
}
```

**Refund calculation rules:**
- Standard passenger: `€10.00` refund
- VIP passenger: `€10.00` refund + a free ice cream voucher 🍦

**Response `200 OK`:**
```json
{
  "refundBatchId": "refund-batch-xyz",
  "totalRefunded": 2,
  "totalAmount": 20.00,
  "voucherCount": 1,
  "processedAt": "2026-01-15T11:05:00Z"
}
```

---

##### `GET /refunds/{rideId}/history`

Returns refund batches for a ride (last 20).

---

## 4. Dapr Workflow Specification

### Workflow: `RideWorkflow`

**Trigger**: `POST /api/rides/{rideId}/start` via Control Center API  
**Input**: `RideWorkflowInput { RideId, WorkflowId, StartedAt }`

#### Steps (Activities)

```
1. CheckRideStatusActivity(rideId)
   └─ Calls Ride Service GET /rides/{rideId}
   └─ Fails workflow if status ≠ Idle

2. Parallel fan-out (all must succeed):
   ├─ CheckWeatherActivity()
   │   └─ Calls Weather Service GET /weather/current
   │   └─ Fails if severity = Severe
   ├─ CheckMascotActivity(rideId)
   │   └─ Calls Mascot Service GET /mascots
   │   └─ Fails if any mascot is in the ride's zone
   └─ (Safety check stub — returns true)

3. LoadPassengersActivity(rideId, capacity)
   └─ Calls Queue Service POST /queue/{rideId}/load
   └─ Records VIP flag in workflow state

4. StartRideActivity(rideId)
   └─ Calls Ride Service POST /rides/{rideId}/start

5. ── RUNNING LOOP ──
   Wait for one of:
   ├─ RideCompletedTimer (configurable, default 90 seconds)
   ├─ External event: WeatherAlertReceived
   ├─ External event: MascotIntrusionReceived
   └─ External event: MalfunctionReceived

   ON WeatherAlertReceived:
   │  ├─ severity = Mild  → PauseRideActivity → WaitFor(WeatherCleared) → ResumeRideActivity → back to loop
   │  └─ severity = Severe → goto FAILURE PATH

   ON MascotIntrusionReceived:
   │  └─ PauseRideActivity → WaitFor(MascotCleared, timeout=5min) → ResumeRideActivity → back to loop

   ON MalfunctionReceived:
   │  └─ TriggerMaintenanceActivity → WaitFor(MaintenanceApproved + maintenance.completed) → ResumeRideActivity → back to loop

   ON RideCompletedTimer:
   └─ goto COMPLETION PATH

6. COMPLETION PATH:
   └─ StopRideActivity(rideId)
   └─ LogCompletionActivity(workflowId, passengersServed)
   └─ Workflow ends with status = Completed

7. FAILURE PATH (compensation):
   └─ PauseRideActivity (if running)
   └─ IssueRefundActivity(rideId, workflowId, passengers)
   └─ TriggerMaintenanceActivity(rideId, reason=Failure)
   └─ StopRideActivity
   └─ Workflow ends with status = Failed
```

### External Events

| Event Name | Raised By | Workflow Reaction |
|---|---|---|
| `WeatherAlertReceived` | Control Center API (after `weather.alert` pub/sub) | See running loop |
| `MascotIntrusionReceived` | Control Center API (after `mascot.in-restricted-zone`) | See running loop |
| `MalfunctionReceived` | Control Center API (after `ride.malfunction`) | See running loop |
| `MaintenanceApproved` | Control Center API (`POST /rides/{id}/maintenance/approve`) | Unblocks maintenance wait |
| `ChaosEventResolved` | Control Center API (`POST /rides/{id}/events/{eventId}/resolve`) | Unblocks weather/mascot wait |

### Workflow Timeouts

| Wait Step | Timeout | On Timeout |
|---|---|---|
| MascotCleared | 5 minutes | Treat as resolved (mascot handler caught it) |
| MaintenanceApproved | 30 minutes | Escalate to FAILURE PATH |
| WeatherCleared | 10 minutes | Re-check; if still Severe → FAILURE PATH |

---

## 5. Event Contracts (Pub/Sub)

All events use Dapr pub/sub with a **Redis** message broker in local development (configured via Aspire).

### `weather.alert`

**Publisher**: Weather Service  
**Subscribers**: Control Center API

```json
{
  "eventId": "uuid",
  "severity": "Mild | Severe",
  "affectedZones": ["Zone-A", "Zone-B"],
  "generatedAt": "ISO8601"
}
```

---

### `mascot.in-restricted-zone`

**Publisher**: Mascot Service  
**Subscribers**: Control Center API

```json
{
  "eventId": "uuid",
  "mascotId": "mascot-001",
  "mascotName": "Roary the Lion",
  "affectedRideId": "uuid",
  "detectedAt": "ISO8601"
}
```

---

### `ride.malfunction`

**Publisher**: Ride Service  
**Subscribers**: Control Center API

```json
{
  "eventId": "uuid",
  "rideId": "uuid",
  "faultCode": "string",
  "description": "string",
  "occurredAt": "ISO8601"
}
```

---

### `maintenance.requested`

**Publisher**: Maintenance Service  
**Subscribers**: Control Center API (for real-time notification to frontend)

```json
{
  "eventId": "uuid",
  "maintenanceId": "string",
  "rideId": "uuid",
  "reason": "string",
  "requestedAt": "ISO8601"
}
```

---

### `maintenance.completed`

**Publisher**: Maintenance Service  
**Subscribers**: Control Center API

```json
{
  "eventId": "uuid",
  "maintenanceId": "string",
  "rideId": "uuid",
  "completedAt": "ISO8601"
}
```

---

### `ride.status-changed`

**Publisher**: Control Center API  
**Subscribers**: Frontend (via SSE stream)

```json
{
  "rideId": "uuid",
  "previousStatus": "Running",
  "newStatus": "Paused",
  "workflowStep": "WaitingForMascotClear",
  "changedAt": "ISO8601"
}
```

---

## 6. Shared Domain Models

These types are defined in `ThemePark.ControlCenter` (the shared core) and referenced across services.

### `RideStatus` (enum)

```csharp
public enum RideStatus
{
    Idle,
    PreFlight,
    Loading,
    Running,
    Paused,
    Maintenance,
    Resuming,
    Completed,
    Failed
}
```

### `ChaosEventType` (enum)

```csharp
public enum ChaosEventType
{
    WeatherAlert,
    MascotIntrusion,
    MechanicalFailure
}
```

### `WeatherSeverity` (enum)

```csharp
public enum WeatherSeverity
{
    Calm,
    Mild,
    Severe
}
```

### `Passenger` (record)

```csharp
public sealed record Passenger(
    string PassengerId,
    string Name,
    bool IsVip);
```

### `RefundReason` (enum)

```csharp
public enum RefundReason
{
    MechanicalFailure,
    WeatherClosure,
    OperationalDecision
}
```

---

## 7. Non-Functional Requirements

### Observability (ADR-0008)

All services must instrument via **OpenTelemetry**:

| Signal | Requirement |
|---|---|
| **Traces** | All HTTP requests, Dapr activity calls, and pub/sub publish/receive operations must be traced. Workflow steps must appear as child spans. |
| **Metrics** | Expose: `rides_started_total`, `rides_completed_total`, `rides_failed_total`, `chaos_events_total{type}`, `refunds_issued_total`, `active_workflows` gauge. |
| **Logs** | Structured JSON logs via `ILogger`. Include `rideId`, `workflowId`, and `step` in all log scopes. |

OTel collector is provided via **Aspire Service Defaults** (`ThemePark.Aspire.ServiceDefaults`).

### Health Checks

Each API must expose:

| Endpoint | Description |
|---|---|
| `GET /health/live` | Returns `200 OK` if the process is alive. |
| `GET /health/ready` | Returns `200 OK` when all dependencies (Dapr sidecar, state store) are reachable. |

### Resilience

| Pattern | Applies To |
|---|---|
| **Retry with exponential backoff** | All Dapr service invocation calls from workflow activities (3 retries, 2s / 4s / 8s). |
| **Timeout** | All workflow activity calls time out after 30 seconds. |
| **Circuit breaker** | Not required for initial implementation. |

### Security

- All inter-service communication uses Dapr's mTLS (enabled by default in Aspire/Dapr mode).
- No sensitive data (passwords, connection strings) may be hard-coded. Use Aspire secrets or environment variables.
- The `POST /rides/{id}/simulate-malfunction`, `POST /weather/simulate`, and `POST /mascots/simulate-intrusion` endpoints are **development-only** and must be guarded by a feature flag (`Dapr:DemoMode: true`) that defaults to `false` in production.

### Performance

- Ride status reads (`GET /api/rides`, `GET /api/rides/{id}/status`) must respond within **200ms** at p95 under 50 concurrent operators.
- SSE connections must support at least **100 concurrent clients** without dropping events.
- The Dapr Workflow must not block the calling API thread; all activity calls use `await`.

---

## 8. Testing Specifications

### Unit Tests

Each service must have a matching `.Tests` project (already scaffolded). Tests use **xUnit** + **Moq** + **Bogus** (per Hexmaster recommendation).

**Minimum coverage targets:**

| Layer | Target |
|---|---|
| Command/Query handlers | 90% line coverage |
| Domain models and state transitions | 100% of state transitions covered |
| Workflow activities (mocked Dapr client) | 80% line coverage |

**Naming convention:** `MethodName_Scenario_ExpectedBehavior`

### Integration Tests

Use `Aspire.Hosting.Testing` to spin up the full Aspire AppHost in test mode.

**Scenarios to cover:**

| Scenario | Assertion |
|---|---|
| Happy path ride session | Workflow reaches `Completed`; no refunds issued |
| Weather mild alert mid-ride | Ride transitions `Running → Paused → Running`; no refunds |
| Mascot intrusion mid-ride | Ride pauses; resumes after operator clears mascot |
| Mechanical failure | Workflow enters maintenance flow; resumes on operator approval |
| Severe weather during pre-flight | Workflow enters `Failed`; refunds issued |
| Operator approves maintenance | `MaintenanceApproved` event unblocks workflow |

### Demo Script (E2E)

The following sequence should work end-to-end and serves as the acceptance test for each sprint:

1. Open Control Center UI → see all 5 rides in `Idle` state.
2. Click **Start** on Thunder Mountain → status changes to `PreFlight` → `Loading` → `Running`.
3. Trigger weather alert (Mild) from demo panel → ride pauses → alert appears in frontend.
4. Click **Clear Weather** → ride resumes.
5. Trigger mascot intrusion → ride pauses → mascot alert shown.
6. Click **Clear Mascot** → ride resumes.
7. Trigger mechanical failure → ride enters `Maintenance` state.
8. Click **Approve Maintenance** → ride eventually returns to `Running`.
9. Wait for ride timer to expire → status changes to `Completed`.
10. Check ride history → session shown with all chaos events logged.
