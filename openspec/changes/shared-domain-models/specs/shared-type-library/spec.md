## ADDED Requirements

### Requirement: RideStatus enum is defined with all operational states
The system SHALL define a `RideStatus` enum in `ThemePark.Shared` containing exactly 9 values: `Idle`, `PreFlight`, `Loading`, `Running`, `Paused`, `Maintenance`, `Resuming`, `Completed`, `Failed`.

#### Scenario: All ride lifecycle states are present
- **WHEN** a developer references `ThemePark.Shared.RideStatus`
- **THEN** the enum exposes exactly `Idle`, `PreFlight`, `Loading`, `Running`, `Paused`, `Maintenance`, `Resuming`, `Completed`, and `Failed`

#### Scenario: RideStatus is used across service boundaries without redefinition
- **WHEN** two services exchange a ride status value via an integration event
- **THEN** both services deserialize the value using the same `RideStatus` enum from `ThemePark.Shared`

### Requirement: ChaosEventType enum is defined with all chaos event categories
The system SHALL define a `ChaosEventType` enum containing exactly 3 values: `WeatherAlert`, `MascotIntrusion`, `MechanicalFailure`.

#### Scenario: All chaos categories are accessible
- **WHEN** a developer references `ThemePark.Shared.ChaosEventType`
- **THEN** the enum exposes exactly `WeatherAlert`, `MascotIntrusion`, and `MechanicalFailure`

### Requirement: WeatherSeverity enum is defined
The system SHALL define a `WeatherSeverity` enum containing exactly 3 values: `Calm`, `Mild`, `Severe`.

#### Scenario: Weather severity levels are accessible
- **WHEN** a developer references `ThemePark.Shared.WeatherSeverity`
- **THEN** the enum exposes exactly `Calm`, `Mild`, and `Severe`

### Requirement: MaintenanceStatus enum is defined
The system SHALL define a `MaintenanceStatus` enum containing exactly 4 values: `Pending`, `InProgress`, `Completed`, `Cancelled`.

#### Scenario: All maintenance lifecycle states are present
- **WHEN** a developer references `ThemePark.Shared.MaintenanceStatus`
- **THEN** the enum exposes exactly `Pending`, `InProgress`, `Completed`, and `Cancelled`

### Requirement: MaintenanceReason enum is defined
The system SHALL define a `MaintenanceReason` enum containing exactly 3 values: `MechanicalFailure`, `ScheduledCheck`, `Failure`.

#### Scenario: All maintenance reason values are present
- **WHEN** a developer references `ThemePark.Shared.MaintenanceReason`
- **THEN** the enum exposes exactly `MechanicalFailure`, `ScheduledCheck`, and `Failure`

### Requirement: RefundReason enum is defined
The system SHALL define a `RefundReason` enum containing exactly 3 values: `MechanicalFailure`, `WeatherClosure`, `OperationalDecision`.

#### Scenario: All refund reason values are present
- **WHEN** a developer references `ThemePark.Shared.RefundReason`
- **THEN** the enum exposes exactly `MechanicalFailure`, `WeatherClosure`, and `OperationalDecision`

### Requirement: ChaosEventResolution enum is defined
The system SHALL define a `ChaosEventResolution` enum containing exactly 3 values: `MascotCleared`, `WeatherCleared`, `SafetyOverride`.

#### Scenario: All resolution values are present
- **WHEN** a developer references `ThemePark.Shared.ChaosEventResolution`
- **THEN** the enum exposes exactly `MascotCleared`, `WeatherCleared`, and `SafetyOverride`

### Requirement: Passenger record is a sealed immutable type
The system SHALL define `public sealed record Passenger(string PassengerId, string Name, bool IsVip)` in `ThemePark.Shared`.

#### Scenario: Passenger instances are value-equal when all properties match
- **WHEN** two `Passenger` instances are created with identical `PassengerId`, `Name`, and `IsVip` values
- **THEN** they are considered equal via the record's built-in value equality

#### Scenario: Passenger is sealed and cannot be subclassed
- **WHEN** a developer attempts to inherit from `Passenger`
- **THEN** the compiler reports an error because `Passenger` is sealed

### Requirement: RideInfo record is a sealed immutable type
The system SHALL define `public sealed record RideInfo(Guid RideId, string Name, int Capacity, string Zone)` in `ThemePark.Shared`.

#### Scenario: RideInfo instances are value-equal when all properties match
- **WHEN** two `RideInfo` instances are created with identical property values
- **THEN** they are considered equal via the record's built-in value equality

#### Scenario: RideInfo Zone is a string identifier matching zone constants
- **WHEN** a `RideInfo` is constructed with `Zone = "Zone-A"`
- **THEN** the `Zone` property returns `"Zone-A"` unchanged

### Requirement: ChaosEvent record is a sealed immutable type
The system SHALL define `public sealed record ChaosEvent(string EventId, ChaosEventType Type, string Severity, DateTimeOffset ReceivedAt, bool Resolved)` in `ThemePark.Shared`.

#### Scenario: ChaosEvent captures all required chaos metadata
- **WHEN** a chaos event is constructed with all five properties
- **THEN** each property is accessible and returns the value it was constructed with

### Requirement: IntegrationEvent base record is defined
The system SHALL define `public abstract record IntegrationEvent(string EventId, DateTimeOffset OccurredAt)` in `ThemePark.Shared`. All pub/sub integration event records SHALL inherit from this base.

#### Scenario: A concrete integration event inherits EventId and OccurredAt
- **WHEN** a service defines `public sealed record RideStartedEvent(Guid RideId) : IntegrationEvent(...)`
- **THEN** the event exposes `EventId` and `OccurredAt` from the base record

#### Scenario: IntegrationEvent cannot be instantiated directly
- **WHEN** a developer attempts to instantiate `IntegrationEvent` directly
- **THEN** the compiler reports an error because the record is abstract

### Requirement: ThemePark.Shared has no infrastructure dependencies
The `ThemePark.Shared` project SHALL reference only the base .NET SDK and MUST NOT include NuGet packages for persistence, serialization, messaging, or any other infrastructure concern.

#### Scenario: ThemePark.Shared project file contains no NuGet package references
- **WHEN** the `ThemePark.Shared.csproj` file is inspected
- **THEN** it contains no `<PackageReference>` elements

### Requirement: All service projects reference ThemePark.Shared
All 7 service projects (ControlCenter, Rides, Queue, Maintenance, Weather, Mascots, Refunds) SHALL have a `<ProjectReference>` to `ThemePark.Shared`.

#### Scenario: Each service can use RideStatus without redefinition
- **WHEN** any service project is compiled
- **THEN** `ThemePark.Shared.RideStatus` is resolvable without a local enum definition
