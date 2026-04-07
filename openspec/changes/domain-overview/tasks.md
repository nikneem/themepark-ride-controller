## 1. Shared Domain Types

- [ ] 1.1 Create `ThemePark.Shared/Domain/Zone.cs` — strongly-typed value object (readonly record struct) with factory method that throws `ArgumentException` for values outside Zone-A, Zone-B, Zone-C
- [ ] 1.2 Create `ThemePark.Shared/Domain/RideStatus.cs` — enum with values: `Idle`, `Loading`, `Running`, `Paused`, `Maintenance`, `Failed`, `Completed`
- [ ] 1.3 Create `ThemePark.Shared/Domain/Passenger.cs` — immutable record with `PassengerId` (Guid), `Name` (string), `IsVip` (bool); add XML doc comments documenting immutability invariant
- [ ] 1.4 Create `ThemePark.Shared/Domain/RideSeedData.cs` — static class exposing all 5 canonical rides with stable GUIDs, names, zones, and capacities; add XML doc comment warning that GUIDs must never change

## 2. Ride Entity and Workflow Convention

- [ ] 2.1 Add XML doc comments to the `Ride` entity (or create `ThemePark.Shared/Domain/Ride.cs` skeleton) documenting all fields: `RideId`, `Name`, `Capacity`, `Zone`, `Status`; reference `core-domain-concepts` spec
- [ ] 2.2 Create `ThemePark.Shared/Workflows/WorkflowIdFactory.cs` — static method `Create(Guid rideId, DateTime utcNow)` producing `ride-{rideId}-{yyyyMMddHHmmss}`; add XML doc comment with format specification

## 3. At-Most-One-Workflow Guard

- [ ] 3.1 In the Control Center service, add a guard in the "start ride" command handler that queries the Dapr Workflow engine (or Rides Service state) to check whether an active workflow already exists for the given `rideId`; return HTTP 409 if one is found
- [ ] 3.2 Add XML doc comment to the guard method referencing the `core-domain-concepts` invariant: "at most one active workflow per ride at any time"

## 4. Zone Constraint Validation

- [ ] 4.1 In the Rides Service, add a validation step on the `RideInfo` input model (or command) that calls `Zone.Parse()` / `Zone.TryParse()` and returns a 400 Bad Request with a descriptive error for invalid zone values
- [ ] 4.2 Verify that ride seeding in the Rides Service calls `RideSeedData` from `ThemePark.Shared` rather than declaring GUIDs inline

## 5. RefundBatch Idempotency

- [ ] 5.1 In the Refund Service, add a `workflowId` uniqueness check before persisting a new `RefundBatch`; if a batch for that `workflowId` already exists, return success without inserting duplicates
- [ ] 5.2 Add XML doc comment on `RefundBatch` documenting the idempotency invariant and referencing the `domain-invariants` spec

## 6. Unit Tests — Invariants

- [ ] 6.1 Add unit test: `Zone_InvalidValue_ThrowsArgumentException` — verifies that constructing a `Zone` with an out-of-range value fails
- [ ] 6.2 Add unit test: `StartRide_WhenAlreadyActive_Returns409` — verifies the at-most-one-workflow guard in the Control Center command handler
- [ ] 6.3 Add unit test: `RefundBatch_DuplicateWorkflowId_DoesNotCreateDuplicate` — verifies idempotency in the Refund Service
- [ ] 6.4 Add unit test: `StartRide_WhenNotIdle_ReturnsError` — verifies that a ride start is rejected when `RideStatus` is not `Idle`
- [ ] 6.5 Add unit test: `Passenger_IsVip_IsImmutableAfterConstruction` — verifies that the `Passenger` record does not expose any mutation path for `IsVip`
