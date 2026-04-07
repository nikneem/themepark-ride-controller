## ADDED Requirements

### Requirement: RideCatalog contains exactly 5 pre-seeded rides with stable GUIDs
The system SHALL define a `public static class RideCatalog` in `ThemePark.Shared` containing exactly 5 `static readonly RideInfo` fields with deterministic, hard-coded GUIDs that MUST NOT change between builds or environments.

The 5 rides are:
| Field Name | Name | Capacity | Zone | GUID |
|---|---|---|---|---|
| `ThunderMountain` | Thunder Mountain | 24 | Zone-A | stable GUID |
| `SpaceCoaster` | Space Coaster | 12 | Zone-A | stable GUID |
| `SplashCanyon` | Splash Canyon | 20 | Zone-B | stable GUID |
| `HauntedMansion` | Haunted Mansion | 16 | Zone-C | stable GUID |
| `DragonsLair` | Dragon's Lair | 8 | Zone-A | stable GUID |

#### Scenario: RideCatalog exposes exactly 5 rides
- **WHEN** `RideCatalog.All` is enumerated
- **THEN** it contains exactly 5 `RideInfo` entries

#### Scenario: Thunder Mountain has correct properties
- **WHEN** `RideCatalog.ThunderMountain` is accessed
- **THEN** `Name` is `"Thunder Mountain"`, `Capacity` is `24`, and `Zone` is `"Zone-A"`

#### Scenario: Space Coaster has correct properties
- **WHEN** `RideCatalog.SpaceCoaster` is accessed
- **THEN** `Name` is `"Space Coaster"`, `Capacity` is `12`, and `Zone` is `"Zone-A"`

#### Scenario: Splash Canyon has correct properties
- **WHEN** `RideCatalog.SplashCanyon` is accessed
- **THEN** `Name` is `"Splash Canyon"`, `Capacity` is `20`, and `Zone` is `"Zone-B"`

#### Scenario: Haunted Mansion has correct properties
- **WHEN** `RideCatalog.HauntedMansion` is accessed
- **THEN** `Name` is `"Haunted Mansion"`, `Capacity` is `16`, and `Zone` is `"Zone-C"`

#### Scenario: Dragon's Lair has correct properties
- **WHEN** `RideCatalog.DragonsLair` is accessed
- **THEN** `Name` is `"Dragon's Lair"`, `Capacity` is `8`, and `Zone` is `"Zone-A"`

### Requirement: Each ride has a stable, unique GUID that does not change between builds
The `RideId` GUID for each ride in `RideCatalog` SHALL be hard-coded as a literal value and MUST NOT be generated at runtime (e.g., no `Guid.NewGuid()`).

#### Scenario: RideId is identical across two independent process starts
- **WHEN** `RideCatalog.ThunderMountain.RideId` is read in two separate process instances
- **THEN** both instances return the same GUID value

#### Scenario: All 5 ride GUIDs are distinct
- **WHEN** all GUIDs from `RideCatalog.All` are collected into a set
- **THEN** the set contains exactly 5 distinct values

### Requirement: Zone mapping is accurate and consistent
`RideCatalog` SHALL accurately reflect the zone assignments: Zone-A contains Thunder Mountain, Space Coaster, and Dragon's Lair; Zone-B contains Splash Canyon; Zone-C contains Haunted Mansion.

#### Scenario: Zone-A rides are correctly identified
- **WHEN** `RideCatalog.All` is filtered by `Zone == "Zone-A"`
- **THEN** the result contains exactly `ThunderMountain`, `SpaceCoaster`, and `DragonsLair`

#### Scenario: Zone-B rides are correctly identified
- **WHEN** `RideCatalog.All` is filtered by `Zone == "Zone-B"`
- **THEN** the result contains exactly `SplashCanyon`

#### Scenario: Zone-C rides are correctly identified
- **WHEN** `RideCatalog.All` is filtered by `Zone == "Zone-C"`
- **THEN** the result contains exactly `HauntedMansion`

### Requirement: RideCatalog.All provides a read-only collection of all rides
`RideCatalog` SHALL expose a `static readonly IReadOnlyList<RideInfo> All` property containing all 5 rides. The collection MUST be immutable at runtime.

#### Scenario: RideCatalog.All cannot be mutated
- **WHEN** a consumer attempts to cast `RideCatalog.All` to `IList<RideInfo>` and add an element
- **THEN** an `NotSupportedException` is thrown at runtime

#### Scenario: RideCatalog.All contains the same instances as named fields
- **WHEN** `RideCatalog.All` is inspected
- **THEN** each entry is value-equal to its corresponding named field (e.g., `RideCatalog.ThunderMountain`)
