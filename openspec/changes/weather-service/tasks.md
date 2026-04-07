## 1. Project & Infrastructure Setup

- [x] 1.1 Create `ThemePark.Weather.Api` ASP.NET Core project targeting .NET 10, port 5104
- [x] 1.2 Add Dapr SDK and Dapr pub/sub NuGet packages; configure Dapr app-id `weather-service`
- [x] 1.3 Register the project in the .NET Aspire AppHost with Dapr sidecar

## 2. Domain Model & Configuration

- [x] 2.1 Define `WeatherSeverity` enum: `Calm`, `Mild`, `Severe`
- [x] 2.2 Define `WeatherCondition` record: `Condition`, `Severity`, `AffectedZones`, `GeneratedAt`
- [x] 2.3 Define `WeatherAlert` record (pub/sub payload): `EventId`, `Severity`, `AffectedZones`, `GeneratedAt`
- [x] 2.4 Add `WeatherOptions` configuration class bound to `Weather:` section (`SimulationIntervalSeconds`, `CalmWeight`, `MildWeight`, `SevereWeight`, `Zones`); register defaults (60s, 60/30/10, Zone-A/B/C) in `appsettings.json`

## 3. Simulation Engine

- [x] 3.1 Implement `IWeatherSimulationEngine` interface with `GenerateCondition()` and `CurrentCondition` property
- [x] 3.2 Implement `WeatherSimulationEngine` singleton: random severity selection using configured weights, random zone assignment for Mild/Severe, initial state Calm
- [x] 3.3 Implement `WeatherSimulationBackgroundService` (`BackgroundService`): uses `PeriodicTimer` at configured interval, calls engine to generate condition, publishes `weather.alert` via Dapr pub/sub for Mild/Severe

## 4. API Endpoints

- [x] 4.1 Implement `GET /weather/current` Minimal API endpoint: returns 200 with current `WeatherCondition` JSON
- [x] 4.2 Implement `POST /weather/simulate` Minimal API endpoint: reads `Dapr:DemoMode` flag — returns 404 if false; validates severity; forces condition on engine; publishes event for Mild/Severe; returns 202

## 5. Tests

- [x] 5.1 Unit test `WeatherSimulationEngine`: probability weights, Calm produces empty zones, Mild/Severe produce non-empty zones, initial state is Calm
- [x] 5.2 Unit test `WeatherSimulationBackgroundService`: Calm does not publish, Mild/Severe publish correct payload
- [x] 5.3 Unit test `POST /weather/simulate`: DemoMode off → 404, invalid severity → 400, Calm → 202 no publish, Mild/Severe → 202 + publish

## 6. Documentation & Cleanup

- [ ] 6.1 Update `README` or service-level docs with endpoint descriptions, Dapr topic, and configuration keys
- [ ] 6.2 Verify OpenTelemetry traces are emitted for timer ticks and pub/sub publishes (ADR-0008 compliance)
