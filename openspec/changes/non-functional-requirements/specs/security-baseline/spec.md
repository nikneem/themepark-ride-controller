## ADDED Requirements

### Requirement: DemoMode feature flag guards simulation endpoints
Demo-mode simulation endpoints (`POST /rides/simulate-malfunction`, `POST /weather/simulate`, `POST /mascots/simulate-intrusion`) SHALL be guarded by the `Dapr:DemoMode` configuration key. When `Dapr:DemoMode` is `false` (the default), the endpoints SHALL return HTTP 403 Forbidden. When `Dapr:DemoMode` is `true`, the endpoints SHALL execute normally.

#### Scenario: Simulation endpoint blocked when DemoMode is false
- **WHEN** `Dapr:DemoMode` is `false` (or not set) and a caller invokes a simulation endpoint
- **THEN** the endpoint returns HTTP 403 Forbidden without executing any side effects

#### Scenario: Simulation endpoint accessible when DemoMode is true
- **WHEN** `Dapr:DemoMode` is `true` and a caller invokes a simulation endpoint
- **THEN** the endpoint executes normally and returns its expected success response

#### Scenario: DemoMode defaults to false
- **WHEN** no `Dapr:DemoMode` configuration value is present
- **THEN** simulation endpoints return HTTP 403 Forbidden (treated as `false`)

---

### Requirement: No hard-coded secrets
The codebase SHALL NOT contain hard-coded connection strings, API keys, passwords, certificates, or any other secrets. All secret values SHALL be supplied via .NET Aspire secrets management or environment variables at runtime.

#### Scenario: Static analysis finds no secrets in source
- **WHEN** the repository is scanned for hard-coded secrets
- **THEN** no secret values are found in any `.cs`, `.json`, or `.yaml` file committed to source control

#### Scenario: Service starts using environment-supplied secret
- **WHEN** a required connection string is provided via an environment variable or Aspire secrets
- **THEN** the service starts successfully and connects to the target resource

---

### Requirement: Inter-service mTLS via Dapr
Inter-service communication SHALL use mutual TLS provided by the Dapr sidecar. No additional application-level TLS configuration is required. Services SHALL NOT disable Dapr mTLS.

#### Scenario: Service-to-service call uses mTLS
- **WHEN** one service invokes another via Dapr service invocation
- **THEN** the communication channel is encrypted and mutually authenticated by the Dapr control plane (no plaintext channel is used)

#### Scenario: mTLS is not disabled in configuration
- **WHEN** any service's Dapr configuration is inspected
- **THEN** mTLS is not explicitly disabled (no `mtls: false` setting is present)
