## ADDED Requirements

### Requirement: GET /weather/current returns the current simulated condition
The service SHALL expose `GET /weather/current`. The response SHALL be HTTP 200 with a JSON body containing: `condition` (string), `severity` (string), `affectedZones` (array of strings), `generatedAt` (ISO-8601 UTC timestamp).

#### Scenario: Returns current condition after simulation has run
- **WHEN** the simulation engine has generated at least one condition and `GET /weather/current` is called
- **THEN** the response is HTTP 200 with the most recently generated `severity`, `affectedZones`, and `generatedAt`

#### Scenario: Returns Calm with empty zones before first tick
- **WHEN** the service has just started and no simulation tick has fired
- **THEN** `GET /weather/current` returns HTTP 200 with `severity: "Calm"` and `affectedZones: []`

---

### Requirement: Response reflects the most recent condition only
The endpoint SHALL return the single most recently generated condition. It SHALL NOT return history or aggregates.

#### Scenario: Subsequent calls reflect updated condition
- **WHEN** the simulation engine generates a new condition after a previous call to `GET /weather/current`
- **THEN** the next call to `GET /weather/current` returns the new condition, not the previous one
