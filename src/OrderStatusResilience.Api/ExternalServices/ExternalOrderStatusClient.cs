using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using OrderStatusResilience.Api.Simulations;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace OrderStatusResilience.Api.ExternalServices;

public sealed class ExternalOrderStatusClient(
    HttpClient httpClient,
    SimulationAttemptTracker attemptTracker,
    ILogger<ExternalOrderStatusClient> logger) : IExternalOrderStatusClient
{
    public async Task<ExternalOrderResult> GetStatusAsync(
        string orderId,
        SimulationScenario scenario,
        CancellationToken cancellationToken)
    {
        var operationId = Guid.NewGuid().ToString("N");
        var path = QueryHelpers.AddQueryString(
            $"external/status/{Uri.EscapeDataString(orderId)}",
            "scenario",
            scenario.ToString());

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(SimulatedExternalOrderHandler.OperationIdHeader, operationId);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadFromJsonAsync<SimulationResponse>(cancellationToken);
            var attempts = attemptTracker.Complete(operationId);

            if (response.IsSuccessStatusCode && payload is not null)
            {
                logger.LogInformation(
                    "Order {OrderId} resolved after {Attempts} attempt(s) in scenario {Scenario}",
                    orderId,
                    attempts,
                    scenario);

                return new ExternalOrderResult(
                    orderId,
                    payload.Status.Status,
                    attempts,
                    scenario,
                    ExternalOrderFailure.None);
            }

            return new ExternalOrderResult(
                orderId,
                null,
                attempts,
                scenario,
                ExternalOrderFailure.DependencyUnavailable,
                payload?.Error ?? "The external order service is unavailable.");
        }
        catch (TimeoutRejectedException)
        {
            return Failure(ExternalOrderFailure.Timeout, "The external order service timed out.");
        }
        catch (BrokenCircuitException)
        {
            return Failure(ExternalOrderFailure.CircuitOpen, "The circuit is open for the external order service.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            attemptTracker.Complete(operationId);
            throw;
        }

        ExternalOrderResult Failure(ExternalOrderFailure failure, string error) =>
            new(
                orderId,
                null,
                attemptTracker.Complete(operationId),
                scenario,
                failure,
                error);
    }
}
