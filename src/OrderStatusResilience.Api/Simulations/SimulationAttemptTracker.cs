using System.Collections.Concurrent;

namespace OrderStatusResilience.Api.Simulations;

public sealed class SimulationAttemptTracker
{
    private readonly ConcurrentDictionary<string, int> _attempts = new();

    public int Record(string operationId) =>
        _attempts.AddOrUpdate(operationId, 1, (_, attempts) => attempts + 1);

    public int Complete(string operationId) =>
        _attempts.TryRemove(operationId, out var attempts) ? attempts : 0;
}
