## 1. Project Scaffolding

- [ ] 1.1 Create `ThemePark.Mascots.Api` .NET 10 Web API project with Dapr and Aspire service defaults references
- [ ] 1.2 Register the mascot-service in the Aspire AppHost with port 5105 and Dapr app-id `mascot-service`
- [ ] 1.3 Add `MascotSimulation:IntervalSeconds` configuration key with default value `45` to `appsettings.json`

## 2. Domain Model

- [ ] 2.1 Create `Mascot` record/class with properties `MascotId`, `Name`, `CurrentZone`, `IsInRestrictedZone`, and `AffectedRideId`
- [ ] 2.2 Define zone constants and helper to classify a zone as restricted (Zone-A, Zone-B, Zone-C) or safe (Park-Central, Backstage)
- [ ] 2.3 Create `MascotInRestrictedZoneEvent` and `MascotClearedEvent` domain event records matching the pub/sub payloads

## 3. In-Memory State Store

- [ ] 3.1 Implement `MascotStateStore` (singleton) using `ConcurrentDictionary`, initialised with mascot-001, mascot-002, and mascot-003 all in `Park-Central`
- [ ] 3.2 Add thread-safe helpers: get all mascots, get single mascot, update zone, check zone occupancy

## 4. Movement Simulation

- [ ] 4.1 Implement `MascotMovementService : IHostedService` that reads the interval from configuration and starts a `PeriodicTimer`
- [ ] 4.2 On each tick, randomly assign each mascot a new zone; skip the assignment if the target zone is already occupied by another mascot
- [ ] 4.3 After each zone update, if the new zone is restricted publish a `mascot.in-restricted-zone` event via Dapr pub/sub

## 5. REST Endpoints

- [ ] 5.1 Implement `GET /mascots` minimal API endpoint returning all mascots from `MascotStateStore`
- [ ] 5.2 Implement `POST /mascots/{mascotId}/clear` endpoint — move mascot to `Park-Central`, publish `mascot.cleared`, return 200 with clear details; return 404 for unknown or non-restricted mascots
- [ ] 5.3 Register `POST /mascots/simulate-intrusion` endpoint only when `Dapr:DemoMode` is `true`; validate `mascotId` and `targetRideId`, immediately update state and publish `mascot.in-restricted-zone`, return 202

## 6. Feature Flag Wiring

- [ ] 6.1 Read `Dapr:DemoMode` from configuration at startup and conditionally register the simulate-intrusion endpoint

## 7. Tests

- [ ] 7.1 Unit test `MascotStateStore`: initialisation, zone update, occupancy check
- [ ] 7.2 Unit test zone classification helper: all five zones return correct `IsInRestrictedZone` and `AffectedRideId`
- [ ] 7.3 Unit test `MascotMovementService`: timer tick moves mascots, skips occupied zones, publishes events only for ride zones
- [ ] 7.4 Unit test clear endpoint handler: success path, 404 for unknown mascot, 404 for non-restricted mascot
- [ ] 7.5 Unit test simulate-intrusion handler: success 202, 400 for unknown mascot ID, 400 for invalid ride ID, event published immediately
