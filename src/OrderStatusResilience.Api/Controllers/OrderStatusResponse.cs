using OrderStatusResilience.Api.Simulations;

namespace OrderStatusResilience.Api.Controllers;

public sealed record OrderStatusResponse(
    string OrderId,
    string Status,
    int Attempts,
    SimulationScenario Scenario);
