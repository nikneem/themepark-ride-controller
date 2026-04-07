## ADDED Requirements

### Requirement: Retry policy on Dapr service-invocation HttpClient
All `HttpClient` instances used for Dapr service invocation SHALL have a retry policy applied via `Microsoft.Extensions.Http.Resilience`. The policy SHALL retry up to 3 times on transient HTTP errors (5xx, 408, network failure) using exponential backoff with delays of 2 s, 4 s, and 8 s.

#### Scenario: Transient 503 triggers retry
- **WHEN** a Dapr service-invocation call receives HTTP 503
- **THEN** the call is retried up to 3 times with delays of approximately 2 s, 4 s, and 8 s before propagating the failure

#### Scenario: Successful response on first retry
- **WHEN** the first attempt fails with HTTP 500 and the second attempt succeeds
- **THEN** the caller receives the successful response and no exception is thrown

#### Scenario: All retries exhausted propagates exception
- **WHEN** all 3 retries fail
- **THEN** the final exception (or `HttpRequestException`) is propagated to the caller

#### Scenario: Non-transient 400 is not retried
- **WHEN** a Dapr service-invocation call receives HTTP 400 (Bad Request)
- **THEN** the error is returned immediately without any retry attempts

---

### Requirement: Activity timeout of 30 seconds
Every workflow activity that calls an external service via HttpClient SHALL have a total request timeout of 30 seconds enforced by the resilience pipeline. If the timeout elapses, a `TimeoutRejectedException` (or equivalent cancellation) SHALL be raised.

#### Scenario: Request completes within timeout
- **WHEN** a Dapr service-invocation call completes in under 30 seconds
- **THEN** the response is returned normally

#### Scenario: Request exceeds timeout raises exception
- **WHEN** a Dapr service-invocation call does not respond within 30 seconds
- **THEN** a timeout exception is raised and propagated to the calling workflow activity

---

### Requirement: Resilience policy registered in ServiceDefaults
`AddServiceDefaults()` SHALL configure the global `HttpClient` resilience defaults (via `ConfigureHttpClientDefaults` / `AddStandardResilienceHandler`) so every `HttpClient` created via `IHttpClientFactory` automatically inherits the retry and timeout policy.

#### Scenario: HttpClient from factory inherits resilience policy
- **WHEN** a service resolves an `HttpClient` via `IHttpClientFactory` after calling `AddServiceDefaults()`
- **THEN** the client has the retry and timeout pipeline applied without any additional per-client configuration
