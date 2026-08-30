namespace OrderStatusResilience.Api.Simulations;

public interface IExternalOrderSimulator
{
    Task<SimulationResponse> GetStatusAsync(
        string orderId,
        SimulationScenario scenario,
        string operationId,
        CancellationToken cancellationToken);
}
