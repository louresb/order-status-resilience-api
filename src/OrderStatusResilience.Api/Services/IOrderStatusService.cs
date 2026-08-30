using OrderStatusResilience.Api.ExternalServices;
using OrderStatusResilience.Api.Simulations;

namespace OrderStatusResilience.Api.Services;

public interface IOrderStatusService
{
    Task<ExternalOrderResult> GetStatusAsync(
        string orderId,
        SimulationScenario scenario,
        CancellationToken cancellationToken);
}
