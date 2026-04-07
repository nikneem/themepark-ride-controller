## 1. Project Scaffolding

- [x] 1.1 Create `ThemePark.Queue.Api` minimal API project targeting .NET 10, add it to the solution, and configure port 5102
- [x] 1.2 Add Dapr SDK, Bogus, and shared domain packages as project references/NuGet dependencies
- [x] 1.3 Register `ThemePark.Queue.Api` in the Aspire AppHost with Dapr sidecar (app-id `queue-service`) and state store binding

## 2. Domain Model

- [x] 2.1 Define `Passenger` record `{ PassengerId (Guid), Name (string), IsVip (bool) }`
- [x] 2.2 Define request/response models: `QueueStateResponse`, `LoadPassengersRequest`, `LoadPassengersResponse`, `SimulateQueueRequest`

## 3. Queue State Endpoint

- [x] 3.1 Implement `GET /queue/{rideId}` handler: read `queue-{rideId}` from Dapr state store, compute `waitingCount`, `hasVip`, and `estimatedWaitMinutes` from configurable `AverageLoadCapacity` and `AverageRideDurationMinutes` settings
- [x] 3.2 Return zeroed response when no queue state exists for the ride

## 4. Load Passengers Endpoint

- [x] 4.1 Implement `POST /queue/{rideId}/load` handler: read queue with ETag, splice off up to `capacity` passengers (FIFO), write remainder back with ETag concurrency check, retry on conflict (max 5 attempts)
- [x] 4.2 Build and return `LoadPassengersResponse` with `passengers`, `loadedCount`, `vipCount`, and `remainingInQueue`

## 5. Simulate Queue Endpoint

- [x] 5.1 Implement `POST /queue/{rideId}/simulate-queue` handler: generate `count` passengers using Bogus (`Faker<Passenger>`) with `isVip` driven by `vipProbability`, replace existing queue in Dapr state store
- [x] 5.2 Register the simulate-queue endpoint only when `Dapr:DemoMode` is `true` in configuration

## 6. Configuration

- [x] 6.1 Add `Queue:AverageLoadCapacity` and `Queue:AverageRideDurationMinutes` to `appsettings.json` with sensible defaults (e.g., 20 and 3)
- [x] 6.2 Add `Dapr:DemoMode` boolean configuration key and wire feature-flag check in endpoint registration

## 7. Tests

- [ ] 7.1 Unit-test `GET /queue/{rideId}` handler: empty queue returns zeros; populated queue returns correct counts and `estimatedWaitMinutes`
- [ ] 7.2 Unit-test `POST /queue/{rideId}/load`: full capacity load, partial load (fewer than capacity), empty queue load, and VIP count accuracy
- [ ] 7.3 Unit-test `POST /queue/{rideId}/simulate-queue`: correct passenger count generated, queue replaced not appended, unique passenger IDs, feature flag off returns 404
