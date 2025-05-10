using OrderStatusResilience.Api.ExternalServices;
using OrderStatusResilienceApi.ExternalServices;

namespace OrderStatusResilienceApi.Services
{
    public class OrderStatusService : IOrderStatusService
    {
        private readonly IExternalOrderStatusClient _externalClient;

        public OrderStatusService(IExternalOrderStatusClient externalClient)
        {
            _externalClient = externalClient;
        }

        public async Task<string> GetOrderStatusAsync(string orderId)
        {
            return await _externalClient.FetchStatusAsync(orderId);
        }
    }
}
