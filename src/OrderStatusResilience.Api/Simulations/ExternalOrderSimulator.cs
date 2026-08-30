using System.Net;

namespace OrderStatusResilience.Api.Simulations;

public sealed class ExternalOrderSimulator(SimulationAttemptTracker attemptTracker)
    : IExternalOrderSimulator
{
    public async Task<SimulationResponse> GetStatusAsync(
        string orderId,
        SimulationScenario scenario,
        string operationId,
        CancellationToken cancellationToken)
    {
        var attempt = attemptTracker.Record(operationId);

        if (scenario == SimulationScenario.Timeout)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

            return Failure(
                HttpStatusCode.GatewayTimeout,
                orderId,
                attempt,
                scenario,
                "The simulated dependency exceeded its response time.");
        }

        if (scenario == SimulationScenario.PersistentFailure ||
            scenario == SimulationScenario.TransientFailure && attempt <= 2)
        {
            return Failure(
                HttpStatusCode.ServiceUnavailable,
                orderId,
                attempt,
                scenario,
                "The simulated dependency is temporarily unavailable.");
        }

        return new SimulationResponse(
            HttpStatusCode.OK,
            new ExternalOrderStatus(orderId, "shipped", attempt, scenario));
    }

    private static SimulationResponse Failure(
        HttpStatusCode statusCode,
        string orderId,
        int attempt,
        SimulationScenario scenario,
        string error) =>
        new(
            statusCode,
            new ExternalOrderStatus(orderId, "unavailable", attempt, scenario),
            error);
}
