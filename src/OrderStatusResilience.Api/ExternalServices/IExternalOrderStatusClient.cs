using OrderStatusResilience.Api.Simulations;

namespace OrderStatusResilience.Api.ExternalServices;

public interface IExternalOrderStatusClient
{
    Task<ExternalOrderResult> GetStatusAsync(
        string orderId,
        SimulationScenario scenario,
        CancellationToken cancellationToken);
}
