# Order Status Resilience API

This project demonstrates how to build a resilient .NET 8 API using `HttpClientFactory`, and [Polly](https://github.com/App-vNext/Polly) to handle instability in external services using proven resilience patterns.

## ✅ Purpose

This PoC simulates integration with an unreliable external system, showcasing key resilience patterns commonly used in production APIs:

- Retry with exponential backoff
- Timeout to avoid hanging requests
- Circuit Breaker to prevent cascading failures
- Fallback with configurable message
- Structured logging and retry tracking
- Health check for upstream dependency

## 🧩 Architecture

The API exposes a resilient endpoint to retrieve the status of an order. Internally, it delegates to a service layer, which interacts with a simulated unstable dependency.

### Flow

```text
GET /order/status/{orderId}
       ↓
OrderStatusService
       ↓
ExternalOrderStatusClient (HttpClient + Polly)
       ↓
Simulated API: /external/status/{orderId}
```

## ⚙️ Resilience Policies

Policies are composed using `Policy.WrapAsync(...)`, demonstrating how multiple layers of protection can be applied to external HTTP calls.

- `WaitAndRetryAsync`: retries transient failures with backoff
- `TimeoutAsync`: aborts calls exceeding 5 seconds
- `CircuitBreakerAsync`: opens the circuit after 2 consecutive failures
- `FallbackAsync`: returns a safe default response when everything else fails

### Retry Example

```csharp
WaitAndRetryAsync(
    retryCount: 3,
    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * attempt)
)
```

### Fallback Behavior

When all else fails, the fallback provides a graceful degradation response:

```json
{
  "orderId": "123",
  "status": "The service is temporarily unavailable. Please try again later."
}
```

Response status: `503 Service Unavailable`

## 🩺 Health Check

The `/health` endpoint checks the availability of the external service via `/external/status/teste` and reports:

- ✅ `Healthy` – response is 200
- ⚠️ `Degraded` – response is an error
- ❌ `Unhealthy` – exception or timeout occurred

## 🚀 Running the Application

```bash
dotnet build
dotnet run --project src/OrderStatusResilience.Api
```

Then open your browser at:

```
https://localhost:{port}/swagger
```

## 📬 Available Endpoints

- `GET /order/status/{orderId}` – resilient endpoint using retry, timeout, circuit breaker and fallback
- `GET /external/status/{orderId}` – simulated unstable dependency (random success or failure)
- `GET /health` – verifies external system availability
