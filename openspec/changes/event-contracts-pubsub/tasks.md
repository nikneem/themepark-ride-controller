## 1. Shared Event Contracts Library

- [x] 1.1 Create `src/Shared/ThemePark.EventContracts/ThemePark.EventContracts.csproj` targeting .NET 10 and add it to the solution
- [x] 1.2 Add `WeatherSeverity` enum (`Calm`, `Mild`, `Severe`) to the library
- [x] 1.3 Add `WeatherAlertEvent` record (`EventId`, `Severity`, `AffectedZones`, `GeneratedAt`) to the library
- [x] 1.4 Add `MascotInRestrictedZoneEvent` record (`EventId`, `MascotId`, `MascotName`, `AffectedRideId`, `DetectedAt`) to the library
- [x] 1.5 Add `RideMalfunctionEvent` record (`EventId`, `RideId`, `FaultCode`, `Description`, `OccurredAt`) to the library
- [x] 1.6 Add `MaintenanceRequestedEvent` record (`EventId`, `MaintenanceId`, `RideId`, `Reason`, `RequestedAt`) to the library
- [x] 1.7 Add `MaintenanceCompletedEvent` record (`EventId`, `MaintenanceId`, `RideId`, `CompletedAt`) to the library
- [x] 1.8 Add `RideStatusChangedEvent` record (`RideId`, `PreviousStatus`, `NewStatus`, `WorkflowStep`, `ChangedAt`) to the library
- [x] 1.9 Configure `JsonSerializerOptions` helper (camelCase + `JsonStringEnumConverter`) in the library and register it via `JsonSerializerContext` or a static options property

## 2. Aspire AppHost — Dapr Pub/Sub Component

- [x] 2.1 Reference `ThemePark.EventContracts` from all publisher and subscriber service projects
- [x] 2.2 Register the `themepark-pubsub` Dapr pub/sub component in Aspire AppHost wired to the existing Redis resource
- [ ] 2.3 Verify the component appears in the Aspire dashboard when running locally

## 3. Control Center API — Subscriber Endpoints

- [x] 3.1 Call `app.MapSubscribeHandler()` in the Control Center API startup pipeline
- [x] 3.2 Add subscriber endpoint for `weather.alert` using `[Topic("themepark-pubsub", "weather.alert", DeadLetterTopic = "weather.alert.deadletter")]`, deserializing into `WeatherAlertEvent`
- [x] 3.3 Add subscriber endpoint for `mascot.in-restricted-zone` using `[Topic("themepark-pubsub", "mascot.in-restricted-zone", DeadLetterTopic = "mascot.in-restricted-zone.deadletter")]`, deserializing into `MascotInRestrictedZoneEvent`
- [x] 3.4 Add subscriber endpoint for `ride.malfunction` using `[Topic("themepark-pubsub", "ride.malfunction", DeadLetterTopic = "ride.malfunction.deadletter")]`, deserializing into `RideMalfunctionEvent`
- [x] 3.5 Add subscriber endpoint for `maintenance.requested` using `[Topic("themepark-pubsub", "maintenance.requested", DeadLetterTopic = "maintenance.requested.deadletter")]`, deserializing into `MaintenanceRequestedEvent` and forwarding to the SSE channel
- [x] 3.6 Add subscriber endpoint for `maintenance.completed` using `[Topic("themepark-pubsub", "maintenance.completed", DeadLetterTopic = "maintenance.completed.deadletter")]`, deserializing into `MaintenanceCompletedEvent` and signalling the waiting Dapr workflow step

## 4. Control Center API — ride.status-changed Publisher

- [ ] 4.1 In `RideWorkflow`, publish a `RideStatusChangedEvent` via `DaprClient.PublishEventAsync("themepark-pubsub", "ride.status-changed", ...)` on every ride status transition
- [ ] 4.2 Ensure `WorkflowStep` is populated with the activity/step name on each publish call
- [ ] 4.3 Wire the `ride.status-changed` event to the SSE channel so connected frontend clients receive the payload in the SSE `data` field

## 5. Publisher Service Updates

- [ ] 5.1 Update Weather Service to publish `WeatherAlertEvent` (from `ThemePark.EventContracts`) to `weather.alert` when severity is `Mild` or `Severe`
- [ ] 5.2 Update Mascot Service to publish `MascotInRestrictedZoneEvent` to `mascot.in-restricted-zone` on intrusion detection
- [ ] 5.3 Update Ride Service to publish `RideMalfunctionEvent` to `ride.malfunction` on fault detection
- [ ] 5.4 Update Maintenance Service to publish `MaintenanceRequestedEvent` to `maintenance.requested` on `POST /maintenance/request`
- [ ] 5.5 Update Maintenance Service to publish `MaintenanceCompletedEvent` to `maintenance.completed` on `POST /maintenance/{id}/complete`

## 6. Integration Tests

- [ ] 6.1 Write an xUnit integration test that publishes a `WeatherAlertEvent` to a test Dapr pub/sub component and asserts the Control Center subscriber endpoint is invoked with the correct deserialized payload
- [ ] 6.2 Write an xUnit integration test for `maintenance.completed` that verifies the waiting workflow step is unblocked after the event is received
- [ ] 6.3 Write a unit test for the `ThemePark.EventContracts` serialisation helper asserting camelCase property names and string enum values in the serialised JSON
- [ ] 6.4 Write a unit test asserting that a `WeatherAlertEvent` with severity `Calm` is never published by Weather Service
