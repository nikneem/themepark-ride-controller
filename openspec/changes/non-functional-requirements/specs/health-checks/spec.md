## ADDED Requirements

### Requirement: Liveness endpoint
Every service API SHALL expose `GET /health/live`. The endpoint SHALL return HTTP 200 and a JSON body indicating the process is alive, unconditionally (no external dependency checks).

#### Scenario: Liveness check always returns 200
- **WHEN** `GET /health/live` is called on any running service
- **THEN** the response is HTTP 200 with a JSON body indicating healthy status

#### Scenario: Liveness check does not depend on Dapr sidecar
- **WHEN** the Dapr sidecar is unavailable but the process is running
- **THEN** `GET /health/live` still returns HTTP 200

---

### Requirement: Readiness endpoint
Every service API SHALL expose `GET /health/ready`. The endpoint SHALL return HTTP 200 only when the Dapr sidecar is reachable at `http://localhost:3500/v1.0/healthz`. If the sidecar is not reachable the endpoint SHALL return HTTP 503.

#### Scenario: Readiness check passes when Dapr sidecar is healthy
- **WHEN** `GET /health/ready` is called and the Dapr sidecar responds 200 to its health endpoint
- **THEN** the response is HTTP 200

#### Scenario: Readiness check fails when Dapr sidecar is unreachable
- **WHEN** `GET /health/ready` is called and the Dapr sidecar is not reachable
- **THEN** the response is HTTP 503 with a JSON body describing the failed check

#### Scenario: Readiness check fails when Dapr sidecar returns non-200
- **WHEN** `GET /health/ready` is called and the Dapr sidecar returns a non-200 status
- **THEN** the response is HTTP 503

---

### Requirement: Health check registration in ServiceDefaults
`AddServiceDefaults()` SHALL register both the liveness and readiness health checks via `IHealthChecksBuilder` so every service that calls `AddServiceDefaults()` automatically gets both endpoints without additional configuration.

#### Scenario: Both endpoints are available after AddServiceDefaults
- **WHEN** a service calls `AddServiceDefaults()` and starts
- **THEN** both `GET /health/live` and `GET /health/ready` return valid responses without any additional health-check configuration in the service
