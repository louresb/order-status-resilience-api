# Order Status Resilience API

[![.NET Build](https://github.com/louresb/order-status-resilience-api/actions/workflows/dotnet-build.yml/badge.svg)](https://github.com/louresb/order-status-resilience-api/actions/workflows/dotnet-build.yml)

Proof of concept for a resilient HTTP integration built with ASP.NET Core and .NET 10. The API retrieves an order status from a simulated dependency and exposes deterministic failure scenarios so each resilience strategy can be observed and tested.

## Resilience strategies

The typed `HttpClient` uses the standard resilience handler from `Microsoft.Extensions.Http.Resilience`:

- Retry with exponential backoff for transient HTTP failures
- Timeout per attempt and a total request timeout
- Circuit breaker to stop calls during repeated failures
- Consistent `503 Service Unavailable` and `504 Gateway Timeout` responses

The simulator runs in process behind an `HttpMessageHandler`. This keeps the example independent from local ports while preserving the same HTTP request, response and resilience pipeline used for a real dependency.

## Request flow

```text
GET /order/status/{orderId}
        |
OrderStatusService
        |
ExternalOrderStatusClient
        |
Standard resilience pipeline
        |
Simulated external order service
```

## Scenarios

Use the `scenario` query parameter to reproduce a specific behavior:

| Scenario | Simulated behavior | Expected result |
| --- | --- | --- |
| `success` | Succeeds immediately | `200`, one attempt |
| `transientFailure` | Fails twice, then succeeds | `200`, three attempts |
| `persistentFailure` | Always returns `503` | `503` after retries |
| `timeout` | Exceeds the timeout on every attempt | `504` after bounded attempts |

Example:

```http
GET /order/status/123?scenario=transientFailure
```

```json
{
  "orderId": "123",
  "status": "shipped",
  "attempts": 3,
  "scenario": "transientFailure"
}
```

Repeated persistent failures open the circuit. While it remains open, requests fail fast without calling the dependency.

## Endpoints

- `GET /order/status/{orderId}` — resilient order-status lookup
- `GET /external/status/{orderId}` — deterministic dependency simulator
- `GET /health` — ASP.NET Core health check for the simulated dependency

## Run locally

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet restore
dotnet run --project src/OrderStatusResilience.Api
```

Open the Swagger UI using the URL shown in the terminal, or run the requests in `OrderStatusResilienceApi.http`.

## Tests

The integration suite verifies success, retry recovery, exhausted retries, timeout, circuit breaker and health-check behavior.

```bash
dotnet test --configuration Release
```

## License

This project is licensed under the [MIT License](LICENSE).
