## Context

The `RideWorkflow` (Dapr Workflow) already handles the happy path of a ride session. When a session ends in failure (mechanical, weather, or operational), the workflow must execute a compensation activity — `IssueRefundActivity` — that calls the Refund Service via Dapr service invocation. Without this service the failure branch of the saga has no observable side-effect for passengers.

The Refund Service is a new, self-contained microservice following the same Vertical Slice Architecture and Minimal API patterns used by other services in this project.

## Goals / Non-Goals

**Goals:**
- Issue monetary refunds (€10.00/passenger) and VIP ice cream vouchers as a Dapr Workflow compensation step
- Expose a POST /refunds endpoint that is idempotent on `workflowId` to survive Dapr Workflow replay
- Expose a GET /refunds/{rideId}/history endpoint returning the last 20 refund batches for a ride
- Persist refund state in the Dapr state store
- Register the service in the .NET Aspire AppHost

**Non-Goals:**
- Actual payment processing or integration with a real payment gateway
- Email/push notification to passengers
- Admin UI or manual refund override
- Voucher redemption tracking

## Decisions

### 1. Idempotency keyed on `workflowId`

**Decision**: Before inserting a new refund batch, look up `refund-history-{rideId}` and check whether any existing batch shares the same `workflowId`. If found, return the stored batch unchanged.

**Rationale**: Dapr Workflow can replay activities on transient failures. Without this guard, passengers could receive multiple refunds for a single ride failure. Keying on `workflowId` (which is stable across replays) is the simplest correct approach given we already persist history per ride.

**Alternative considered**: A separate `workflowId → batchId` index key in the state store — rejected as it doubles state operations for every POST with no additional benefit at this scale.

---

### 2. VIP voucher as a flag in the response, not a separate service call

**Decision**: The response includes a `voucherCount` integer (count of VIP passengers) rather than making a downstream call to a voucher service.

**Rationale**: There is no voucher service in the demo. Representing the voucher as a count keeps the compensation path synchronous and observable without introducing a new dependency that could fail and block the saga.

**Alternative considered**: Publishing a `voucher.issued` Dapr pub/sub event — deferred to a future change; would require a subscriber service to be meaningful.

---

### 3. Total amount calculated server-side

**Decision**: The server computes `totalAmount` from the passenger list (€10.00 × passenger count). Callers do not supply an amount.

**Rationale**: Prevents callers (including the workflow) from manipulating the refund amount. The calculation rule is simple and stable.

---

### 4. History capped at 20 per ride in state store

**Decision**: When appending to `refund-history-{rideId}`, keep only the 20 most-recent entries (by `processedAt` desc).

**Rationale**: The state store key grows unboundedly otherwise. 20 entries is more than sufficient for the conference demo and keeps GET /history responses fast without pagination.

---

### 5. Minimal API + CQRS handlers (vertical slice)

**Decision**: Follow ADR-0005 (Minimal APIs) and ADR-0007 (Vertical Slice / feature folders). Each endpoint maps to a command or query handler via MediatR.

**Rationale**: Consistent with all other services in the project.

## Risks / Trade-offs

- **Dapr state store race condition on concurrent POSTs for the same rideId** → Mitigated by the fact that `RideWorkflow` is a single-threaded Dapr Workflow; only one compensation activity fires per workflow instance. Acceptable for demo scope.
- **History list loaded/saved on every POST** → Simple read-modify-write on the list is acceptable at demo scale; a production system would use a database with atomic append.
- **No authentication on endpoints** → Consistent with the rest of the demo project. Out of scope.

## Migration Plan

1. Add `ThemePark.Refunds.Api` project to the solution
2. Register in Aspire AppHost alongside existing services
3. Wire `IssueRefundActivity` in `RideWorkflow` to call `POST /refunds` via Dapr service invocation
4. Deploy — no data migration required (new state keys)
5. Rollback: remove AppHost registration; `IssueRefundActivity` becomes a no-op stub

## Open Questions

- Should the `IssueRefundActivity` in `RideWorkflow` treat a non-2xx response from the Refund Service as a retryable or terminal failure? (Recommendation: retryable with exponential backoff, relying on idempotency to prevent duplicates.)
