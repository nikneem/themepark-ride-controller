## ADDED Requirements

### Requirement: Ride session must start from Idle state only
The system SHALL reject any attempt to start a ride session unless the ride's current status is `Idle`. Starting a session from any other status (e.g. `Running`, `Paused`, `Maintenance`, `Failed`) MUST result in an error.

#### Scenario: Session starts successfully from Idle
- **WHEN** an operator requests a ride start and the ride status is `Idle`
- **THEN** the workflow is created and the ride transitions to `Loading` (or the initial non-Idle state)

#### Scenario: Session start rejected when not Idle
- **WHEN** an operator requests a ride start and the ride status is `Running`
- **THEN** the request is rejected with an error indicating the ride is not in a startable state

#### Scenario: Session start rejected when in Maintenance
- **WHEN** an operator requests a ride start and the ride status is `Maintenance`
- **THEN** the request is rejected with an error indicating the ride is not in a startable state

---

### Requirement: Passenger list is immutable after LoadPassengersActivity completes
The system SHALL freeze the passenger list for a ride session once `LoadPassengersActivity` has completed. No passengers may be added to or removed from the session after this point.

#### Scenario: Passenger list is locked after loading completes
- **WHEN** `LoadPassengersActivity` has completed for a session
- **THEN** any subsequent attempt to modify the passenger list for that session is rejected

#### Scenario: Passenger count used for refunds equals boarding count
- **WHEN** a session ends in the `Failed` state after `LoadPassengersActivity` completed
- **THEN** refunds are issued for exactly the passengers recorded at the time boarding completed — no more, no fewer

---

### Requirement: Refund batch is idempotent per workflowId
The system SHALL ensure that processing a refund request for a given `workflowId` more than once produces exactly the same outcome as processing it once. A second invocation for the same `workflowId` MUST NOT create duplicate refund records.

#### Scenario: First refund request is processed
- **WHEN** a refund batch is submitted for `workflowId = "ride-a1b2c3d4-0001-0000-0000-000000000001-20250115143022"`
- **THEN** refund records are created for all passengers in that session

#### Scenario: Duplicate refund request is ignored
- **WHEN** a refund batch is submitted for a `workflowId` that has already been processed
- **THEN** no new refund records are created and the operation returns success without error

#### Scenario: Workflow replay does not double-issue refunds
- **WHEN** a Dapr Workflow replays due to a pod restart and re-executes the `IssueRefundsActivity`
- **THEN** the idempotency check on `workflowId` prevents duplicate refunds from being persisted

---

### Requirement: VIP status is determined at boarding and cannot change during a session
The system SHALL set the `isVip` flag on each `Passenger` record at the time of boarding (when `LoadPassengersActivity` executes). The flag MUST NOT be modified at any point after that for the duration of the session.

#### Scenario: VIP passenger receives ice cream voucher on failure
- **WHEN** a session ends in the `Failed` state and a passenger has `isVip = true`
- **THEN** that passenger's refund record includes an ice cream voucher in addition to the standard €10.00 refund

#### Scenario: Non-VIP passenger does not receive voucher
- **WHEN** a session ends in the `Failed` state and a passenger has `isVip = false`
- **THEN** that passenger's refund record contains only the standard €10.00 refund without a voucher

#### Scenario: VIP flag cannot be changed mid-session
- **WHEN** an external event or message attempts to change a passenger's `isVip` flag after boarding has completed
- **THEN** the system rejects or ignores the change and the original `isVip` value is preserved

---

### Requirement: Chaos events require operator resolution or timeout — they cannot silently disappear
The system SHALL ensure that every active chaos event on a ride session is resolved either by an explicit operator action or by the designated timeout (5-minute auto-clear for `MascotClear` only). No chaos event MAY be removed from a session without one of these two resolution paths.

#### Scenario: Operator resolves a WeatherAlert
- **WHEN** an operator sends a resolution command for an active `WeatherAlert`
- **THEN** the chaos event is marked resolved and the ride session continues

#### Scenario: MascotIntrusion auto-clears after 5 minutes
- **WHEN** a `MascotClear` event is raised and 5 minutes elapse without operator intervention
- **THEN** the chaos event is automatically resolved and the ride session continues

#### Scenario: MechanicalFailure does not auto-resolve
- **WHEN** a `MechanicalFailure` chaos event is active
- **THEN** the event remains active until an operator explicitly approves maintenance and resolves it — it does not time out automatically

#### Scenario: Chaos event cannot disappear without resolution
- **WHEN** a chaos event is active on a session
- **THEN** the session cannot progress to the next lifecycle state until the chaos event is resolved by operator action or the applicable timeout
