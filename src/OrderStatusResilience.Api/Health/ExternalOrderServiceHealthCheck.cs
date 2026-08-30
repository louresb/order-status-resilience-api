using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.WebUtilities;
using OrderStatusResilience.Api.Simulations;

namespace OrderStatusResilience.Api.Health;

public sealed class ExternalOrderServiceHealthCheck(
    HttpClient httpClient,
    SimulationAttemptTracker attemptTracker) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid().ToString("N");
        var path = QueryHelpers.AddQueryString(
            "external/status/health-check",
            "scenario",
            SimulationScenario.Success.ToString());

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(SimulatedExternalOrderHandler.OperationIdHeader, operationId);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("The simulated external order service is responding.")
                : HealthCheckResult.Unhealthy(
                    $"The simulated external order service returned HTTP {(int)response.StatusCode}.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy(
                "The simulated external order service could not be reached.",
                exception);
        }
        finally
        {
            attemptTracker.Complete(operationId);
        }
    }
}
