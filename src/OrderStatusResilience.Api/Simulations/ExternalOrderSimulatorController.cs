using Microsoft.AspNetCore.Mvc;

namespace OrderStatusResilience.Api.Simulations
{
    [ApiController]
    [Route("external/status")]
    public class ExternalOrderSimulatorController : ControllerBase
    {
        private static readonly Random _random = new();

        [HttpGet("{orderId}")]
        public async Task<IActionResult> Get(string orderId)
        {
            await Task.Delay(_random.Next(100, 1000));

            var shouldFail = _random.NextDouble() < 0.5;

            if (shouldFail)
            {
                return StatusCode(500, "Simulated external system failure.");
            }

            return Ok($"Order {orderId}: delivered successfully.");
        }
    }
}
