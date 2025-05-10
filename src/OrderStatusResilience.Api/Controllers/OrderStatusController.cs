using Microsoft.AspNetCore.Mvc;
using OrderStatusResilienceApi.Services;

namespace OrderStatusResilienceApi.Controllers
{
    [ApiController]
    [Route("order")]
    public class OrderStatusController : ControllerBase
    {
        private readonly IOrderStatusService _orderStatusService;

        public OrderStatusController(IOrderStatusService orderStatusService)
        {
            _orderStatusService = orderStatusService;
        }

        [HttpGet("status/{orderId}")]
        public async Task<IActionResult> GetStatus(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                return BadRequest("Order ID cannot be empty.");

            var status = await _orderStatusService.GetOrderStatusAsync(orderId);

            return Ok(new
            {
                orderId,
                status
            });
        }
    }
}
