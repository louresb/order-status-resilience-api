using OrderStatusResilience.Api.ExternalServices;
using OrderStatusResilience.Api.Simulations;

namespace OrderStatusResilience.Api.Services;

public sealed class OrderStatusService(IExternalOrderStatusClient externalClient) : IOrderStatusService
{
    public Task<ExternalOrderResult> GetStatusAsync(
        string orderId,
        SimulationScenario scenario,
        CancellationToken cancellationToken) =>
        externalClient.GetStatusAsync(orderId, scenario, cancellationToken);
}
