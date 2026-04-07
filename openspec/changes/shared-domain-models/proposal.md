## Why

With 7 microservices sharing concepts like `RideStatus`, `Passenger`, and `ChaosEventType`, each service risks defining its own incompatible version. A single shared library establishes the ubiquitous language of the system and prevents semantic divergence across service boundaries.

## What Changes

- Create new `ThemePark.Shared` class library project (pure C#, no infrastructure dependencies)
- Define all shared enums: `RideStatus`, `ChaosEventType`, `WeatherSeverity`, `MaintenanceStatus`, `MaintenanceReason`, `RefundReason`, `ChaosEventResolution`
- Define shared records: `Passenger`, `RideInfo`, `ChaosEvent` (all sealed)
- Define `IntegrationEvent` base record with `EventId` and `OccurredAt` — all pub/sub event records inherit from this
- Add `RideCatalog` static class with 5 pre-seeded rides (stable GUIDs for local dev reproducibility)
- Add project reference to `ThemePark.Shared` from all 7 service projects
- **BREAKING**: Any existing ad hoc type definitions (e.g., `RideOperationalStatus`) are replaced by the canonical types in `ThemePark.Shared`

## Capabilities

### New Capabilities

- `shared-type-library`: All shared enum and record types plus the `IntegrationEvent` base record, constituting the ubiquitous language of the theme park domain
- `ride-seed-data`: Static `RideCatalog` class providing deterministic ride constants (GUIDs, names, capacities, zones) for use across all services at startup

### Modified Capabilities

## Impact

- All 7 service projects: ControlCenter, Rides, Queue, Maintenance, Weather, Mascots, Refunds
- `ThemePark.Aspire.AppHost` for ride seed data reference
- Any existing placeholder types in service projects are superseded
