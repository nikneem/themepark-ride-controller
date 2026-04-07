## 1. Project & Domain Setup

- [ ] 1.1 Create `ThemePark.Rides` domain library project and add it to the solution; define `RideStatus` enum (`Idle`, `Running`, `Paused`, `Maintenance`, `Resuming`, `Completed`, `Failed`)
- [ ] 1.2 Define `RideState` record/class in `ThemePark.Rides` with properties: `RideId`, `Name`, `OperationalStatus`, `Capacity`, `CurrentPassengerCount`, `PauseReason`
- [ ] 1.3 Create `ThemePark.Rides.Api` minimal API project targeting .NET 10; add project reference to `ThemePark.Rides`; configure Dapr, OpenTelemetry, and Aspire service defaults

## 2. Dapr State Store Integration

- [ ] 2.1 Add the Dapr client SDK NuGet package to `ThemePark.Rides.Api`; register `DaprClient` in DI
- [ ] 2.2 Implement `IRideStateStore` abstraction and `DaprRideStateStore` in `_Shared/`; encapsulate all state store reads/writes using key `ride-state-{rideId}`

## 3. Startup Ride Seeding

- [ ] 3.1 Implement `RideSeedService : IHostedService` that writes the 5 default rides (Thunder Mountain cap 24, Space Coaster cap 12, Splash Canyon cap 20, Haunted Mansion cap 16, Dragon's Lair cap 8) to the state store on startup, skipping any ride whose key already exists

## 4. GetRide Endpoint

- [ ] 4.1 Implement `GetRide/` slice: handler reads ride state from `IRideStateStore`, returns 200 with `RideStateResponse` DTO or 404 if not found
- [ ] 4.2 Register `GET /rides/{rideId}` minimal API endpoint in `GetRide/GetRideEndpoint.cs`

## 5. StartRide Endpoint

- [ ] 5.1 Implement `StartRide/` slice: handler reads current state, returns 404 if missing, 409 if status is not `Idle`, otherwise writes `Running` status and returns 200
- [ ] 5.2 Register `POST /rides/{rideId}/start` minimal API endpoint

## 6. PauseRide Endpoint

- [ ] 6.1 Implement `PauseRide/` slice: handler validates `reason` (400 if missing), reads current state, returns 409 if status is not `Running`, otherwise writes `Paused` status with reason and returns 200
- [ ] 6.2 Register `POST /rides/{rideId}/pause` minimal API endpoint; define `PauseRideRequest` with `Reason` property

## 7. ResumeRide Endpoint

- [ ] 7.1 Implement `ResumeRide/` slice: handler reads current state, returns 409 if status is not `Paused`, otherwise writes `Running` status and returns 200
- [ ] 7.2 Register `POST /rides/{rideId}/resume` minimal API endpoint

## 8. StopRide Endpoint

- [ ] 8.1 Implement `StopRide/` slice: handler reads current state, returns 409 if status is `Maintenance`, otherwise writes `Idle` status and returns 200
- [ ] 8.2 Register `POST /rides/{rideId}/stop` minimal API endpoint

## 9. SimulateMalfunction Endpoint

- [ ] 9.1 Implement `SimulateMalfunction/` slice: read `Dapr:DemoMode` config flag; return 404 if disabled; return 404 if ride not found; publish `ride.malfunction` event to Dapr pub/sub with payload `{ rideId, rideName, malfunctionTimestamp }` (UTC ISO-8601) and return 200
- [ ] 9.2 Register `POST /rides/{rideId}/simulate-malfunction` minimal API endpoint, conditionally available based on `Dapr:DemoMode`

## 10. Unit Tests

- [ ] 10.1 Add `ThemePark.Rides.Api.Tests` xUnit project; add references to Moq and Bogus; add project reference to `ThemePark.Rides.Api`
- [ ] 10.2 Write unit tests for `GetRide` handler: returns 200 with correct DTO, returns 404 when ride missing
- [ ] 10.3 Write unit tests for `StartRide` handler: returns 200 from Idle, returns 409 from Running/Paused, returns 404 when missing
- [ ] 10.4 Write unit tests for `PauseRide` handler: returns 200 from Running, returns 409 from Idle/Paused, returns 400 on missing reason
- [ ] 10.5 Write unit tests for `ResumeRide` handler: returns 200 from Paused, returns 409 from Running/Idle
- [ ] 10.6 Write unit tests for `StopRide` handler: returns 200 from Running/Paused, returns 409 from Maintenance
- [ ] 10.7 Write unit tests for `SimulateMalfunction` handler: publishes event when DemoMode=true, returns 404 when DemoMode=false, returns 404 when ride not found
- [ ] 10.8 Write unit tests for `RideSeedService`: seeds all 5 rides on empty store, skips existing rides on restart
