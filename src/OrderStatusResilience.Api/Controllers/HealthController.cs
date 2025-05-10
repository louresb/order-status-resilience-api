using Microsoft.AspNetCore.Mvc;

namespace OrderStatusResilience.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HealthController> _logger;

        public HealthController(IHttpClientFactory httpClientFactory, ILogger<HealthController> logger)
        {
            _httpClient = httpClientFactory.CreateClient("healthcheck");
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var dependencyUrl = "/external/status/test"; 
            string status;
            string message;

            try
            {
                var response = await _httpClient.GetAsync(dependencyUrl);

                if (response.IsSuccessStatusCode)
                {
                    status = "Healthy";
                    message = "External system is responding successfully.";
                }
                else
                {
                    status = "Degraded";
                    message = "External system returned an error.";
                }
            }
            catch (Exception ex)
            {
                status = "Unhealthy";
                message = "Failed to contact external system.";
                _logger.LogError(ex, "Health check failed when calling {Url}", dependencyUrl);
            }

            return Ok(new
            {
                status,
                dependency = dependencyUrl,
                message,
                timestamp = DateTime.UtcNow.ToString("o")
            });
        }
    }
}
