using OrderStatusResilience.Api.Simulations;

namespace OrderStatusResilience.Api.ExternalServices;

public enum ExternalOrderFailure
{
    None,
    DependencyUnavailable,
    Timeout,
    CircuitOpen
}

public sealed record ExternalOrderResult(
    string OrderId,
    string? Status,
    int Attempts,
    SimulationScenario Scenario,
    ExternalOrderFailure Failure,
    string? Error = null)
{
    public bool IsSuccess => Failure == ExternalOrderFailure.None;
}
