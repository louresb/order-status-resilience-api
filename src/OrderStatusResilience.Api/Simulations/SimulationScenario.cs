namespace OrderStatusResilience.Api.Simulations;

public enum SimulationScenario
{
    Success,
    TransientFailure,
    PersistentFailure,
    Timeout
}
