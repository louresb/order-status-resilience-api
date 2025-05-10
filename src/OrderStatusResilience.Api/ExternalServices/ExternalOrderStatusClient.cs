using System.Diagnostics;
using OrderStatusResilienceApi.ExternalServices;
using OrderStatusResilience.Api.Services;

namespace OrderStatusResilience.Api.ExternalServices
{
    public class ExternalOrderStatusClient : IExternalOrderStatusClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ExternalOrderStatusClient> _logger;
        private readonly IRetryTracker _retryTracker;

        public ExternalOrderStatusClient(HttpClient httpClient, ILogger<ExternalOrderStatusClient> logger, IRetryTracker retryTracker)
        {
            _httpClient = httpClient;
            _logger = logger;
            _retryTracker = retryTracker;
        }

        public async Task<string> FetchStatusAsync(string orderId)
        {
            var stopwatch = Stopwatch.StartNew();

            var context = new Polly.Context();
            context["orderId"] = orderId;

            HttpResponseMessage response;

            try
            {
                response = await _httpClient.GetAsync($"/external/status/{orderId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while performing request to {OrderId}", orderId);
                throw;
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Request to /external/status/{OrderId} took {Duration}ms",
                orderId,
                stopwatch.ElapsedMilliseconds
            );

            int attempts = _retryTracker.GetAttempts(orderId);
            _logger.LogInformation("Total retry attempts for {OrderId}: {Attempts}", orderId, attempts);

            if (!response.IsSuccessStatusCode)
            {
                return $"Failed to retrieve status for order {orderId}";
            }

            return await response.Content.ReadAsStringAsync();
        }
    }
}
