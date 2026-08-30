namespace OrderStatusResilience.Api.Simulations;

public sealed record ExternalOrderStatus(
    string OrderId,
    string Status,
    int Attempt,
    SimulationScenario Scenario);
