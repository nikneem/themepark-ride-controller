# 🧩 Microservices Architecture

## Overview

The Theme Park Control Center is built as a set of loosely coupled microservices, each owning a single domain of the theme park. All services are **Daprized** — they communicate via Dapr's service invocation and pub/sub building blocks, and participate in workflows orchestrated by the **Control Center API**.

```
┌─────────────────────────────────────────────────────┐
│                  Control Center UI                  │
│                  (Angular frontend)                 │
└───────────────────────┬─────────────────────────────┘
                        │ HTTP / SignalR
┌───────────────────────▼─────────────────────────────┐
│              Control Center API                     │
│         (ASP.NET Core + Dapr Workflows)             │
└──┬──────────┬──────────┬──────────┬──────────┬──────┘
   │          │          │          │          │
   ▼          ▼          ▼          ▼          ▼
 Ride      Queue    Maintenance  Weather   Mascot
Service   Service    Service    Service   Service
                                               │
                                        Refund Service
                                   (triggered on failure)
```

---

## Services

### 1. Control Center API

**Role:** Workflow orchestrator and API gateway for the frontend.

**Responsibilities:**
- Expose the HTTP API consumed by the frontend
- Instantiate and manage Dapr Workflow instances per ride
- Forward operator actions (approve maintenance, clear mascot, etc.) as external events into running workflows
- Push real-time status updates to the frontend via SignalR

**Key endpoints:**

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/rides/{rideId}/start` | Start a ride workflow |
| `POST` | `/api/rides/{rideId}/maintenance/approve` | Signal workflow: maintenance approved |
| `POST` | `/api/rides/{rideId}/events/{eventId}/resolve` | Signal workflow: chaos event resolved |
| `GET`  | `/api/rides/{rideId}/status` | Get current workflow state |

**Dapr building blocks used:**
- Workflow (orchestrator)
- Service invocation (calls downstream services)
- Pub/sub (subscribes to chaos events from Weather, Mascot, Maintenance services)

---

### 2. Ride Service

**Role:** Controls the physical state of a ride.

**Responsibilities:**
- Start a ride
- Stop or pause a ride
- Report current operational status
- Emit malfunction events when a mechanical fault is detected

**Key operations (invoked by workflow):**

| Operation | Description |
|-----------|-------------|
| `StartRide` | Puts the ride into running state |
| `PauseRide` | Temporarily halts the ride (e.g. during a chaos event) |
| `ResumeRide` | Resumes a paused ride |
| `StopRide` | Ends the ride and marks it idle |
| `GetStatus` | Returns current operational status |

**Events published:**
- `ride.malfunction` — emitted when a mechanical failure is detected

**Dapr building blocks used:**
- Service invocation (receives calls from the workflow)
- Pub/sub (publishes malfunction events)

---

### 3. Queue Service

**Role:** Manages the passenger queue for each ride.

**Responsibilities:**
- Return the current number of waiting passengers
- Provide a passenger manifest for loading
- Flag VIP guests in the queue

**Key operations:**

| Operation | Description |
|-----------|-------------|
| `GetQueueLength` | Returns number of passengers waiting |
| `LoadPassengers` | Returns a passenger list and clears the queue |
| `IsVipPresent` | Returns whether a VIP guest is in the queue |

**Dapr building blocks used:**
- Service invocation (receives calls from the workflow)
- State store (persists queue state)

---

### 4. Maintenance Service

**Role:** Handles repair requests and tracks maintenance history.

**Responsibilities:**
- Receive a maintenance request triggered by a malfunction
- Track repair status
- Notify the workflow when repair is complete

**Key operations:**

| Operation | Description |
|-----------|-------------|
| `RequestMaintenance` | Logs a repair request for a ride |
| `CompleteRepair` | Marks a repair as done; triggers workflow resume |
| `GetMaintenanceHistory` | Returns past maintenance records |

**Events published:**
- `maintenance.requested` — notifies operators a repair is needed
- `maintenance.completed` — triggers workflow to resume after repair

**Dapr building blocks used:**
- Service invocation
- Pub/sub
- State store

---

### 5. Weather Service

**Role:** Simulates weather conditions and emits weather alerts.

**Responsibilities:**
- Periodically generate weather conditions for the park
- Emit alerts when conditions cross safe thresholds

**Weather event levels:**

| Level | Description | Workflow reaction |
|-------|-------------|-------------------|
| `calm` | Normal conditions | No action |
| `mild` | Light rain / wind | Slow ride speed |
| `severe` | Storm / high wind | Emergency stop |

**Events published:**
- `weather.alert` — payload includes severity level and affected zones

**Dapr building blocks used:**
- Pub/sub (publishes weather alerts consumed by the workflow)

---

### 6. Mascot Service

**Role:** Tracks the location of park mascots and raises alerts when they stray into restricted zones.

**Responsibilities:**
- Simulate mascot movement around the park
- Detect when a mascot enters a ride zone
- Emit an alert and wait for operator clearance

**Key operations:**

| Operation | Description |
|-----------|-------------|
| `GetMascotLocations` | Returns current locations of all mascots |
| `ClearMascot` | Marks a mascot as cleared from a restricted zone |

**Events published:**
- `mascot.in-restricted-zone` — emitted when a mascot wanders onto a ride track

**Dapr building blocks used:**
- Service invocation (pre-flight location check from workflow)
- Pub/sub (publishes zone intrusion events)

---

### 7. Refund Service

**Role:** Issues passenger refunds or compensation when a ride is cancelled or fails.

**Responsibilities:**
- Calculate refund amounts based on cancellation reason
- Issue refunds to affected passengers
- Optionally issue in-park compensation (free ice cream 🍦)

**Key operations:**

| Operation | Description |
|-----------|-------------|
| `IssueRefund` | Processes a refund for a passenger list |
| `IssueCompensation` | Issues a non-monetary compensation voucher |

**Triggered by:** Compensation steps in the ride workflow when a failure cannot be recovered.

**Dapr building blocks used:**
- Service invocation (called as a compensation activity by the workflow)

---

## Communication Patterns

### Service Invocation (Request/Response)
Used when the workflow needs a synchronous result from a downstream service — e.g. checking queue length, starting a ride, or issuing a refund.

```
Workflow Activity → Dapr Sidecar → Target Service Sidecar → Target Service
```

### Pub/Sub (Event-Driven)
Used when external services need to push events into the system asynchronously — e.g. a weather alert or mascot intrusion. The Control Center API subscribes to these topics and injects them into the appropriate workflow as external events.

```
Weather Service → Dapr Topic: weather.alert → Control Center API → Workflow.RaiseEvent()
```

### External Events (Workflow)
Operator actions from the UI are forwarded by the Control Center API as external events into the waiting workflow instance. This is how human-in-the-loop steps (approve maintenance, clear mascot) work.

```
Frontend → POST /api/rides/{id}/maintenance/approve
        → Control Center API → Dapr Workflow.RaiseEventAsync("MaintenanceApproved")
        → Workflow resumes
```

---

## Service Boundaries Summary

| Service | Owns | Publishes | Subscribes |
|---------|------|-----------|------------|
| Control Center API | Workflow instances, API surface | `ride.status-changed` | `weather.alert`, `mascot.in-restricted-zone`, `maintenance.requested` |
| Ride Service | Ride operational state | `ride.malfunction` | — |
| Queue Service | Passenger queues | — | — |
| Maintenance Service | Repair records | `maintenance.requested`, `maintenance.completed` | — |
| Weather Service | Weather simulation | `weather.alert` | — |
| Mascot Service | Mascot locations | `mascot.in-restricted-zone` | — |
| Refund Service | Refund transactions | — | — |
