using Microsoft.AspNetCore.Mvc;

namespace OrderStatusResilience.Api.Simulations;

[ApiController]
[Route("external/status")]
public sealed class ExternalOrderSimulatorController(IExternalOrderSimulator simulator) : ControllerBase
{
    [HttpGet("{orderId}")]
    [ProducesResponseType<SimulationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<SimulationResponse>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<SimulationResponse>(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<SimulationResponse>> GetStatus(
        string orderId,
        [FromQuery] SimulationScenario scenario = SimulationScenario.Success,
        [FromQuery] string? operationId = null,
        CancellationToken cancellationToken = default)
    {
        var response = await simulator.GetStatusAsync(
            orderId,
            scenario,
            operationId ?? Guid.NewGuid().ToString("N"),
            cancellationToken);

        return StatusCode((int)response.StatusCode, response);
    }
}
