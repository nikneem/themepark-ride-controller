## ADDED Requirements

### Requirement: Issue batch refund
The system SHALL accept a POST /refunds request containing a ride identifier, workflow identifier, refund reason, and a list of passengers, and SHALL create a refund batch record with a generated `refundBatchId` (GUID), computing the total amount at €10.00 per passenger and counting one ice cream voucher per VIP passenger.

#### Scenario: Standard passenger receives €10.00 refund
- **WHEN** a POST /refunds request is submitted with one non-VIP passenger
- **THEN** the response contains `totalRefunded: 1`, `totalAmount: 10.00`, and `voucherCount: 0`

#### Scenario: VIP passenger receives €10.00 refund and one voucher
- **WHEN** a POST /refunds request is submitted with one VIP passenger (`isVip: true`)
- **THEN** the response contains `totalRefunded: 1`, `totalAmount: 10.00`, and `voucherCount: 1`

#### Scenario: Mixed passenger list
- **WHEN** a POST /refunds request contains three passengers of which two are VIP
- **THEN** the response contains `totalRefunded: 3`, `totalAmount: 30.00`, and `voucherCount: 2`

#### Scenario: Response includes generated batch identifier
- **WHEN** a POST /refunds request is processed successfully
- **THEN** the response contains a non-empty `refundBatchId` GUID and a `processedAt` timestamp
- **AND** the HTTP status code is 200

---

### Requirement: Idempotent refund on workflowId
The system SHALL return the existing refund batch when a POST /refunds request is received with a `workflowId` that already has a recorded batch for the same `rideId`, without creating a duplicate or modifying the existing record.

#### Scenario: Duplicate POST for the same workflowId returns existing batch
- **WHEN** a POST /refunds request is submitted with a `workflowId` that was already processed for the given `rideId`
- **THEN** the system returns the original `refundBatchId`, `totalAmount`, `voucherCount`, and `processedAt` unchanged
- **AND** the HTTP status code is 200

#### Scenario: Different workflowId on same rideId creates new batch
- **WHEN** a POST /refunds request is submitted for a `rideId` with a `workflowId` that has not been seen before
- **THEN** a new `refundBatchId` is generated and the new batch is persisted

---

### Requirement: Persist refund batch in state store
The system SHALL persist each new refund batch in the Dapr state store under key `refund-batch-{refundBatchId}` and SHALL append the batch summary to the history list stored under key `refund-history-{rideId}`, keeping at most 20 entries (most recent first).

#### Scenario: Batch is stored under its own key
- **WHEN** a new refund batch is successfully created
- **THEN** the Dapr state store contains an entry with key `refund-batch-{refundBatchId}` holding the full batch record

#### Scenario: Batch summary is appended to ride history
- **WHEN** a new refund batch is successfully created
- **THEN** the Dapr state store entry `refund-history-{rideId}` contains the new batch summary as the most-recent item

#### Scenario: History is capped at 20 entries
- **WHEN** a new refund batch is created and the ride already has 20 history entries
- **THEN** the oldest entry is removed so the list contains exactly 20 entries

---

### Requirement: Accepted refund reasons
The system SHALL only accept the following values for the `reason` field: `MechanicalFailure`, `WeatherClosure`, `OperationalDecision`. Requests with any other value SHALL be rejected.

#### Scenario: Valid reason is accepted
- **WHEN** a POST /refunds request contains `reason: "MechanicalFailure"`
- **THEN** the request is processed and HTTP 200 is returned

#### Scenario: Invalid reason is rejected
- **WHEN** a POST /refunds request contains an unrecognised `reason` value
- **THEN** the system returns HTTP 400 with an error message indicating the invalid reason

---

### Requirement: Retrieve refund history for a ride
The system SHALL expose a GET /refunds/{rideId}/history endpoint that returns the last 20 refund batches for the specified ride, ordered most-recent first, each containing `refundBatchId`, `workflowId`, `reason`, `totalRefunded`, `totalAmount`, `voucherCount`, and `processedAt`.

#### Scenario: History returned for a ride with batches
- **WHEN** a GET /refunds/{rideId}/history request is made for a ride that has at least one refund batch
- **THEN** the response is an array of batch summaries ordered most-recent first
- **AND** the HTTP status code is 200

#### Scenario: Empty array returned for ride with no history
- **WHEN** a GET /refunds/{rideId}/history request is made for a ride that has no refund batches
- **THEN** the response is an empty array with HTTP status code 200

#### Scenario: At most 20 entries returned
- **WHEN** a GET /refunds/{rideId}/history request is made for a ride that has had more than 20 refund batches over its lifetime
- **THEN** at most 20 entries are returned
