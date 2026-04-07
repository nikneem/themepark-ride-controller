## ADDED Requirements

### Requirement: Retrieve current queue state for a ride
The system SHALL expose a `GET /queue/{rideId}` endpoint that returns the current queue state for the specified ride. The response SHALL include `rideId`, `waitingCount`, `hasVip`, and `estimatedWaitMinutes`. `estimatedWaitMinutes` SHALL be calculated as `waitingCount / averageLoadCapacity * averageRideDurationMinutes`, where `averageLoadCapacity` and `averageRideDurationMinutes` are configurable values. When no queue entry exists for the ride, `waitingCount` SHALL be `0`, `hasVip` SHALL be `false`, and `estimatedWaitMinutes` SHALL be `0`.

#### Scenario: Queue exists with waiting passengers
- **WHEN** a client calls `GET /queue/{rideId}` and the queue for that ride contains passengers
- **THEN** the response is HTTP 200 with `waitingCount` equal to the number of passengers in the queue, `hasVip` true if any passenger has `isVip = true`, and `estimatedWaitMinutes` calculated from the formula

#### Scenario: Queue is empty
- **WHEN** a client calls `GET /queue/{rideId}` and no queue state exists for that ride
- **THEN** the response is HTTP 200 with `waitingCount = 0`, `hasVip = false`, and `estimatedWaitMinutes = 0`

#### Scenario: Queue exists but all passengers are non-VIP
- **WHEN** a client calls `GET /queue/{rideId}` and the queue contains only passengers with `isVip = false`
- **THEN** the response is HTTP 200 with `hasVip = false`

---

### Requirement: Load passengers from a ride queue
The system SHALL expose a `POST /queue/{rideId}/load` endpoint that atomically dequeues up to `capacity` passengers from the front of the queue and returns a boarding manifest. The request body SHALL include `capacity` (integer, minimum 1). The response SHALL include `passengers` (list of `{ passengerId, name, isVip }`), `loadedCount`, `vipCount`, and `remainingInQueue`. Passengers SHALL be dequeued in FIFO order. The operation SHALL be atomic — concurrent load requests for the same ride SHALL NOT return the same passenger.

#### Scenario: Successful load with full capacity available
- **WHEN** a client posts `{ "capacity": 4 }` to `POST /queue/{rideId}/load` and the queue contains 6 passengers
- **THEN** the response is HTTP 200 with `loadedCount = 4`, `passengers` containing the first 4 passengers in queue order, `remainingInQueue = 2`, and `vipCount` equal to the number of VIP passengers among the 4 loaded

#### Scenario: Load with fewer passengers than capacity
- **WHEN** a client posts `{ "capacity": 10 }` to `POST /queue/{rideId}/load` and the queue contains only 3 passengers
- **THEN** the response is HTTP 200 with `loadedCount = 3`, `passengers` containing all 3 passengers, and `remainingInQueue = 0`

#### Scenario: Load from an empty queue
- **WHEN** a client posts `{ "capacity": 4 }` to `POST /queue/{rideId}/load` and the queue is empty or does not exist
- **THEN** the response is HTTP 200 with `loadedCount = 0`, `passengers = []`, `vipCount = 0`, and `remainingInQueue = 0`

#### Scenario: VIP count is correctly reported
- **WHEN** a client loads passengers and 2 of the loaded passengers have `isVip = true`
- **THEN** the response includes `vipCount = 2`

#### Scenario: Concurrent load requests do not double-dequeue
- **WHEN** two concurrent `POST /queue/{rideId}/load` requests are issued for the same ride
- **THEN** each passenger appears in at most one response, and together the two responses contain no duplicates
