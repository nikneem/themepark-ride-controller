## ADDED Requirements

### Requirement: POST /weather/simulate is guarded by the DemoMode feature flag
The `POST /weather/simulate` endpoint SHALL only be available when `Dapr:DemoMode` is `true`. When `Dapr:DemoMode` is `false` or absent, the endpoint SHALL return HTTP 404.

#### Scenario: Endpoint available in demo mode
- **WHEN** `Dapr:DemoMode` is `true` and `POST /weather/simulate` is called with a valid body
- **THEN** the response is HTTP 202 Accepted

#### Scenario: Endpoint unavailable outside demo mode
- **WHEN** `Dapr:DemoMode` is `false` or not configured and `POST /weather/simulate` is called
- **THEN** the response is HTTP 404 Not Found

---

### Requirement: simulate endpoint accepts valid severity values
The request body SHALL contain `severity` (one of: `"Calm"`, `"Mild"`, `"Severe"`) and `affectedZones` (array of strings). The engine SHALL immediately update the current condition and, if severity is Mild or Severe, publish a `weather.alert` event.

#### Scenario: Valid Severe simulate request
- **WHEN** `POST /weather/simulate` is called with `{ "severity": "Severe", "affectedZones": ["Zone-A"] }`
- **THEN** the response is HTTP 202, the current condition becomes Severe with affectedZones `["Zone-A"]`, and a `weather.alert` event is published

#### Scenario: Valid Mild simulate request
- **WHEN** `POST /weather/simulate` is called with `{ "severity": "Mild", "affectedZones": ["Zone-B", "Zone-C"] }`
- **THEN** the response is HTTP 202, the current condition becomes Mild, and a `weather.alert` event is published

#### Scenario: Valid Calm simulate request does not publish event
- **WHEN** `POST /weather/simulate` is called with `{ "severity": "Calm", "affectedZones": [] }`
- **THEN** the response is HTTP 202, the current condition becomes Calm, and no `weather.alert` event is published

---

### Requirement: simulate endpoint rejects invalid severity values
If `severity` is not one of the accepted values, the endpoint SHALL return HTTP 400 Bad Request.

#### Scenario: Invalid severity rejected
- **WHEN** `POST /weather/simulate` is called with `{ "severity": "Hurricane", "affectedZones": [] }`
- **THEN** the response is HTTP 400 Bad Request

---

### Requirement: simulate endpoint overrides the next natural timer result
After a manual simulate call the in-memory current condition SHALL reflect the forced value. The timer loop continues unchanged and will overwrite the condition on the next tick.

#### Scenario: Manual trigger updates current condition immediately
- **WHEN** `POST /weather/simulate` is called with severity Severe
- **THEN** `GET /weather/current` immediately returns severity Severe before the next timer tick fires
