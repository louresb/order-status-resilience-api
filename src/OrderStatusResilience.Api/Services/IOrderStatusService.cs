namespace OrderStatusResilienceApi.Services
{
    public interface IOrderStatusService
    {
        Task<string> GetOrderStatusAsync(string orderId);
    }
}
