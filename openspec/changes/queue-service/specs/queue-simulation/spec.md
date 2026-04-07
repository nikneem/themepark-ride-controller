## ADDED Requirements

### Requirement: Seed a ride queue with simulated passengers
The system SHALL expose a `POST /queue/{rideId}/simulate-queue` endpoint that replaces the current queue for the specified ride with a set of randomly generated passengers. The request body SHALL accept `count` (integer, minimum 1) and `vipProbability` (float between 0 and 1, default `0.1`). Each generated passenger SHALL have a unique `passengerId` (GUID), a realistic randomly generated `name`, and `isVip` set to `true` with probability `vipProbability`. This endpoint SHALL only be available when the `Dapr:DemoMode` feature flag is `true`. Calling this endpoint SHALL replace any previously seeded queue for the ride (idempotent seed — not additive).

#### Scenario: Successful queue seeding
- **WHEN** `Dapr:DemoMode` is `true` and a client posts `{ "count": 20, "vipProbability": 0.2 }` to `POST /queue/{rideId}/simulate-queue`
- **THEN** the response is HTTP 200, the queue for that ride contains exactly 20 passengers, and approximately 20% of them have `isVip = true`

#### Scenario: Default VIP probability applied when omitted
- **WHEN** `Dapr:DemoMode` is `true` and a client posts `{ "count": 50 }` to `POST /queue/{rideId}/simulate-queue` without specifying `vipProbability`
- **THEN** the response is HTTP 200 and the queue is seeded with 50 passengers using a default `vipProbability` of `0.1`

#### Scenario: Existing queue is replaced not appended
- **WHEN** `Dapr:DemoMode` is `true` and a ride already has 30 passengers in queue, and a client posts `{ "count": 10 }` to `POST /queue/{rideId}/simulate-queue`
- **THEN** the queue contains exactly 10 passengers after the call (the previous 30 are discarded)

#### Scenario: Endpoint unavailable when DemoMode is disabled
- **WHEN** `Dapr:DemoMode` is `false` and a client calls `POST /queue/{rideId}/simulate-queue`
- **THEN** the response is HTTP 404 (the endpoint is not registered)

#### Scenario: Each generated passenger has a unique identifier
- **WHEN** `Dapr:DemoMode` is `true` and a client seeds a queue with `count = 100`
- **THEN** all 100 passengers in the resulting queue have distinct `passengerId` values
