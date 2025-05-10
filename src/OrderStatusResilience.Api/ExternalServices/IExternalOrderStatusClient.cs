namespace OrderStatusResilienceApi.ExternalServices
{
    public interface IExternalOrderStatusClient
    {
        Task<string> FetchStatusAsync(string orderId);
    }
}
