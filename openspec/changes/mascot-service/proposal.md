## Why

Mascots provide a fun, visual chaos event for the conference demo. A mascot wandering onto a ride track forces a ride pause and requires an operator to clear it — creating a compelling live demo moment that showcases the system's safety response capabilities.

## What Changes

- Add `ThemePark.Mascots.Api` service (port 5105, Dapr app-id `mascot-service`)
- Three mascots simulated: mascot-001 "Roary the Lion 🦁", mascot-002 "Bella the Bear 🐻", mascot-003 "Ziggy the Zebra 🦓"
- In-memory position tracking across five zones (Park-Central, Zone-A, Zone-B, Zone-C, Backstage)
- Internal timer moves mascots randomly every 45 seconds (configurable)
- Publishes `mascot.in-restricted-zone` when a mascot enters Zone-A, Zone-B, or Zone-C
- REST endpoint to clear a mascot from a restricted zone, publishing `mascot.cleared`
- Demo endpoint (feature-flagged) to immediately force a mascot into a ride zone

## Capabilities

### New Capabilities

- `mascot-tracking`: Position tracking for all mascots, restricted zone detection, and operator clear endpoint
- `mascot-movement-simulation`: Internal timer-driven random movement between zones with pub/sub event on ride zone entry
- `mascot-intrusion-trigger`: Feature-flagged demo endpoint to immediately move a mascot into a specified ride zone

### Modified Capabilities

## Impact

- New .NET Aspire resource registered in AppHost
- Dapr pub/sub topics: `mascot.in-restricted-zone`, `mascot.cleared`
- Ride Controller service may subscribe to `mascot.in-restricted-zone` to trigger ride pauses
- No database dependencies; state is ephemeral (resets on restart)
- Feature flag `Dapr:DemoMode` gates simulate-intrusion endpoint
