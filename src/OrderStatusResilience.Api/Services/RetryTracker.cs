using System.Collections.Concurrent;

namespace OrderStatusResilience.Api.Services
{
    public interface IRetryTracker
    {
        void Increment(string key);
        int GetAttempts(string key);
    }

    public class RetryTracker : IRetryTracker
    {
        private readonly ConcurrentDictionary<string, int> _retryCounts = new();

        public void Increment(string key)
        {
            _retryCounts.AddOrUpdate(key, 1, (_, current) => current + 1);
        }

        public int GetAttempts(string key)
        {
            return _retryCounts.TryGetValue(key, out var value) ? value : 0;
        }
    }
}
