# 🎡 Theme Park Control Center — App Concept

## High-Level Story

You run a futuristic theme park where every ride is controlled by microservices. The **Control Center UI** lets operators:

- Start rides
- Monitor ride progress
- Respond to chaos events
- Approve maintenance
- View workflow history

The **Backend API** orchestrates everything using **Dapr Workflows**.

This provides a perfect blend of fun narrative and real engineering patterns — ideal as a demo for a 1-hour talk.

---

## Architecture Overview

### Frontend (Blazor / React / Vue)

- Dashboard showing all rides and their statuses
- "Start Ride" button per ride
- Live ride progress timeline
- Alerts for malfunctions, weather events, and mascot intrusions
- Operator action buttons (approve maintenance, override safety, clear mascot, etc.)

### Backend API (ASP.NET Core)

Exposes endpoints such as:

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/rides/{rideId}/start` | Start a ride workflow |
| `POST` | `/api/rides/{rideId}/maintenance/approve` | Approve a maintenance request |
| `POST` | `/api/rides/{rideId}/events/{eventId}/resolve` | Resolve a chaos event |
| `GET`  | `/api/rides/{rideId}/status` | Get current ride status |

Each endpoint interacts with the Dapr Workflow runtime.

### Dapr Workflow (Orchestration Core)

The heart of the system. Orchestrates:

- Ride lifecycle (start → run → end)
- Safety and pre-flight checks
- Maintenance sub-workflows
- Chaos event handling and branching
- Compensating transactions (refunds, closures)

### Supporting Microservices (all Daprized)

| Service | Responsibility |
|---------|----------------|
| **Ride Service** | Start and stop rides |
| **Queue Service** | Simulate passenger queues |
| **Maintenance Service** | Handle repair requests |
| **Weather Service** | Emit random weather events |
| **Mascot Service** | Track mascots wandering into restricted zones |
| **Refund Service** | Issue refunds or free ice cream 🍦 |

---

## The Ride Workflow (Step-by-Step)

### 1. Start Ride
Triggered by: Frontend → Backend API → Dapr Workflow

Steps:
- Validate ride availability
- Check queue length
- Run in parallel:
  - Safety check
  - Weather check
  - Mascot location check

### 2. Load Passengers
- Queue Service returns passenger list
- Random chance of VIP guest → triggers special handling branch

### 3. Begin Ride
- Ride Service starts the ride
- Workflow enters a waiting state, listening for external events (chaos events)

### 4. Chaos Event Handling
Events can arrive from Weather Service, Mascot Service, Maintenance Service, or manual operator input.

Workflow reactions:
- Pause ride
- Trigger maintenance sub-workflow
- Request operator approval (external event wait)
- Reroute passengers
- Issue refunds

### 5. End Ride
- Ride Service stops the ride
- Passengers unload
- Workflow logs completion

### 6. Compensations (on failure)
If the ride fails mid-workflow:
- Refund Service issues refunds
- Maintenance workflow is triggered
- Frontend shows "Ride Closed" status

---

## Chaos Events

These make the demo memorable and show off Dapr's external event handling.

### Mascot on the Track 🦁
- Mascot Service emits event
- Workflow pauses the ride
- Operator clicks **"Clear Mascot"**
- Workflow resumes

### Sudden Weather Alert ⛈️
- Weather Service emits "Magical Storm Incoming"
- Workflow branches:
  - Mild → slow the ride
  - Severe → emergency stop

### Mechanical Failure 🔧
- Ride Service reports malfunction
- Workflow triggers maintenance sub-workflow
- Operator must **approve repair** before resuming

### VIP Guest Request ⭐
- VIP wants extra ride time
- Workflow sends approval request to operator
- On approval → ride duration extends

---

## Workflow Activities (Dapr)

Each activity maps to a microservice call:

- `CheckRideStatusActivity`
- `CheckWeatherActivity`
- `CheckMascotActivity`
- `LoadPassengersActivity`
- `StartRideActivity`
- `PauseRideActivity`
- `ResumeRideActivity`
- `TriggerMaintenanceActivity`
- `IssueRefundActivity`
- `LogEventActivity`

---

## Frontend UI Concepts

### Control Center Dashboard
- List of all rides
- Status indicators: `Running`, `Paused`, `Maintenance`, `Error`, `Closed`
- Live event feed (real-time updates)

### Ride Detail View
- Timeline showing each workflow step
- Current step highlighted
- Chaos events appear as alert banners
- Operator action buttons contextual to current state

### Operator Actions
| Action | Trigger |
|--------|---------|
| Approve maintenance | Mechanical failure event |
| Clear mascot | Mascot-on-track event |
| Override safety | Manual operator decision |
| Issue refund | Ride cancellation |
| Restart ride | After resolution of failure |

---

## Why This Demo Works

- **Visually engaging** — real-time status updates and chaos events
- **Technically rich** — showcases Dapr Workflows, pub/sub, service invocation
- **Shows real patterns** — compensation, external events, parallel activities, sub-workflows
- **Great storytelling** — fun theme park narrative makes patterns easy to explain
- **Easy to extend** — add new rides, new chaos events, new microservices
- **Frontend + backend** — full-stack demonstration
