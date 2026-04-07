## Why

Without a documented domain model, each of the 7 microservices risks implementing its own interpretation of core concepts, leading to divergent terminology, mismatched invariants, and integration bugs. This change codifies the ubiquitous language — entities, aggregate roots, and domain rules — so every service shares an identical vocabulary from day one.

## What Changes

- Document all core domain entities: Ride, RideSession, Passenger, Refund, Zone, ParkMascot, ChaosEvent, and Operator
- Define aggregate roots and their ownership boundaries across services
- Codify domain invariants that all service implementations must enforce
- Publish the 5 stable ride seed records with their canonical GUIDs, zones, and capacities
- Establish the workflowId naming convention: `ride-{rideId}-{yyyyMMddHHmmss}`

## Capabilities

### New Capabilities

- `core-domain-concepts`: Entities, aggregate roots, value objects, and ubiquitous language that bind all 7 services together
- `domain-invariants`: Rules that must hold across all services — immutability constraints, idempotency guarantees, and lifecycle guards

### Modified Capabilities

_(none — this is a greenfield domain model with no prior specs)_

## Impact

- All 7 microservices (reference documentation; must align terminology to this spec)
- `ThemePark.Shared` project (invariants and entity definitions inform type design)
- All subsequent changes depend on this vocabulary being settled before implementation begins
