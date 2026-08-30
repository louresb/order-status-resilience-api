using System.Net;

namespace OrderStatusResilience.Api.Simulations;

public sealed record SimulationResponse(
    HttpStatusCode StatusCode,
    ExternalOrderStatus Status,
    string? Error = null);
