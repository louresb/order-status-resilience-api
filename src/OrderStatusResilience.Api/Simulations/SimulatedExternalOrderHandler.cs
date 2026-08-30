using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace OrderStatusResilience.Api.Simulations;

public sealed class SimulatedExternalOrderHandler(IExternalOrderSimulator simulator) : HttpMessageHandler
{
    public const string OperationIdHeader = "X-Operation-Id";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var query = QueryHelpers.ParseQuery(request.RequestUri?.Query ?? string.Empty);
        var scenario = query.TryGetValue("scenario", out var scenarioValue) &&
                       Enum.TryParse<SimulationScenario>(scenarioValue, true, out var parsedScenario)
            ? parsedScenario
            : SimulationScenario.Success;

        var orderId = request.RequestUri?.Segments.LastOrDefault()?.Trim('/') ?? "unknown";
        var operationId = request.Headers.TryGetValues(OperationIdHeader, out var values)
            ? values.Single()
            : Guid.NewGuid().ToString("N");

        var simulation = await simulator.GetStatusAsync(
            Uri.UnescapeDataString(orderId),
            scenario,
            operationId,
            cancellationToken);

        return new HttpResponseMessage(simulation.StatusCode)
        {
            RequestMessage = request,
            Content = JsonContent.Create(simulation)
        };
    }
}
