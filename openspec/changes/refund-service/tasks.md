## 1. Project Scaffold

- [x] 1.1 Create `src/ThemePark.Refunds.Api` project (Minimal API, .NET 10) and add it to the solution
- [x] 1.2 Add NuGet references: Dapr.AspNetCore, MediatR, OpenTelemetry packages consistent with other services
- [x] 1.3 Register `refund-service` in the Aspire AppHost with Dapr sidecar and port 5106

## 2. Domain Models

- [x] 2.1 Define `RefundBatch` record (refundBatchId, rideId, workflowId, reason, passengers, totalRefunded, totalAmount, voucherCount, processedAt)
- [x] 2.2 Define `RefundReason` enum (MechanicalFailure, WeatherClosure, OperationalDecision)
- [x] 2.3 Define request/response DTOs for POST /refunds and GET /refunds/{rideId}/history

## 3. Issue Refund Feature (POST /refunds)

- [x] 3.1 Implement `IssueRefundCommand` and `IssueRefundCommandHandler` (calculate totalAmount, voucherCount; idempotency check on workflowId; persist batch and update history)
- [x] 3.2 Map POST /refunds Minimal API endpoint to `IssueRefundCommand` via MediatR
- [x] 3.3 Validate `reason` field — return HTTP 400 for unrecognised values

## 4. Refund History Feature (GET /refunds/{rideId}/history)

- [x] 4.1 Implement `GetRefundHistoryQuery` and `GetRefundHistoryQueryHandler` (read `refund-history-{rideId}` from Dapr state store; return empty list if not found)
- [x] 4.2 Map GET /refunds/{rideId}/history Minimal API endpoint to `GetRefundHistoryQuery` via MediatR

## 5. State Store Integration

- [x] 5.1 Persist new batch under key `refund-batch-{refundBatchId}` in Dapr state store
- [x] 5.2 Read-modify-write `refund-history-{rideId}` list, prepend new summary, trim to 20 entries

## 6. RideWorkflow Integration

- [x] 6.1 Implement `IssueRefundActivity` Dapr Workflow activity that calls POST /refunds on `refund-service` via Dapr service invocation
- [x] 6.2 Wire `IssueRefundActivity` into `RideWorkflow` as the compensation step on failure exit

## 7. Tests

- [x] 7.1 Unit test `IssueRefundCommandHandler`: standard passenger amount, VIP voucher count, mixed list, idempotency (duplicate workflowId returns existing batch), history cap at 20
- [x] 7.2 Unit test `GetRefundHistoryQueryHandler`: returns empty list when no history, returns ordered list when history exists
