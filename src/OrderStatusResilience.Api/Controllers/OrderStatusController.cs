using Microsoft.AspNetCore.Mvc;
using OrderStatusResilience.Api.ExternalServices;
using OrderStatusResilience.Api.Services;
using OrderStatusResilience.Api.Simulations;

namespace OrderStatusResilience.Api.Controllers;

[ApiController]
[Route("order/status")]
public sealed class OrderStatusController(IOrderStatusService orderStatusService) : ControllerBase
{
    [HttpGet("{orderId}")]
    [ProducesResponseType<OrderStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<OrderStatusResponse>> GetStatus(
        string orderId,
        [FromQuery] SimulationScenario scenario = SimulationScenario.Success,
        CancellationToken cancellationToken = default)
    {
        var result = await orderStatusService.GetStatusAsync(orderId, scenario, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(new OrderStatusResponse(
                result.OrderId,
                result.Status!,
                result.Attempts,
                result.Scenario));
        }

        var (statusCode, title) = result.Failure switch
        {
            ExternalOrderFailure.Timeout =>
                (StatusCodes.Status504GatewayTimeout, "External order service timed out"),
            ExternalOrderFailure.CircuitOpen =>
                (StatusCodes.Status503ServiceUnavailable, "External order service circuit is open"),
            _ =>
                (StatusCodes.Status503ServiceUnavailable, "External order service is unavailable")
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = result.Error
        };

        problem.Extensions["orderId"] = result.OrderId;
        problem.Extensions["scenario"] = result.Scenario;
        problem.Extensions["attempts"] = result.Attempts;

        return StatusCode(statusCode, problem);
    }
}
