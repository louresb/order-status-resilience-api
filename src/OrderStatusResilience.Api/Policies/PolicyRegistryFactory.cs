using OrderStatusResilience.Api.Services;
using Polly;
using Polly.Registry;
using System.Net;

namespace OrderStatusResilienceApi.Policies
{
    public static class PolicyRegistryFactory
    {
        public static IPolicyRegistry<string> Create(IServiceProvider services)
        {
            var registry = new PolicyRegistry();

            var config = services.GetRequiredService<IConfiguration>();
            var logger = services.GetRequiredService<ILogger<object>>();
            var tracker = services.GetRequiredService<IRetryTracker>();

            var fallbackMessage = config["FallbackMessage"] ?? "Service temporarily unavailable (fallback)";

            var retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * attempt),
                    onRetryAsync: (outcome, timespan, attempt, context) =>
                    {
                        if (context.TryGetValue("orderId", out var id) && id is string orderId)
                        {
                            tracker.Increment(orderId);
                        }

                        logger.LogWarning("Retry #{Attempt} executed. Delay: {Delay}ms", attempt, timespan.TotalMilliseconds);
                        return Task.CompletedTask;
                    });

            var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(5);

            var circuitBreakerPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 2,
                    durationOfBreak: TimeSpan.FromSeconds(15)
                );

            var fallbackPolicy = Policy<HttpResponseMessage>
                .Handle<Exception>()
                .FallbackAsync(
                    fallbackAction: (result, context, ct) =>
                    {
                        logger.LogWarning("Fallback triggered for context: {Context}", context.TryGetValue("orderId", out var id) ? id : "unknown");
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                        {
                            Content = new StringContent(fallbackMessage)
                        });
                    },
                    onFallbackAsync: (result, context) =>
                    {
                        logger.LogError(result.Exception, "[Fallback] Exception detected: {Message}", result.Exception?.Message);
                        return Task.CompletedTask;
                    });

            var resiliencePolicy = Policy.WrapAsync(fallbackPolicy, retryPolicy, circuitBreakerPolicy, timeoutPolicy);

            registry.Add("ResiliencePolicy", resiliencePolicy);
            return registry;
        }
    }
}
