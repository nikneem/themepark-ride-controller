## Why

The Dapr Workflow (`RideWorkflow`) needs a compensation path when a ride session ends in failure — without the Refund Service, the saga pattern has no observable outcome beyond a status change and passengers receive no feedback. The Refund Service closes that loop by issuing monetary refunds and VIP vouchers as the final compensation activity.

## What Changes

- New microservice `ThemePark.Refunds.Api` (port 5106, Dapr app-id `refund-service`)
- New `IssueRefundActivity` integration point consumed by `RideWorkflow` as a Dapr Workflow compensation activity
- Batch refund issuance with per-passenger VIP ice cream voucher logic
- Idempotent POST on `workflowId` to prevent double-refunds on Dapr Workflow replay
- Refund history per ride capped at 20 batches, persisted in Dapr state store

## Capabilities

### New Capabilities

- `refund-processing`: Batch refund issuance (POST /refunds) with standard/VIP rules, idempotency on workflowId, and ride history retrieval (GET /refunds/{rideId}/history)

### Modified Capabilities

## Impact

- **New project**: `src/ThemePark.Refunds.Api` — Minimal API, CQRS handlers, Dapr state store
- **RideWorkflow**: wired to call `IssueRefundActivity` → `refund-service` via Dapr service invocation
- **Aspire AppHost**: registers `refund-service` resource
- **Dependencies**: Dapr sidecar, Redis state store (dev), OpenTelemetry
