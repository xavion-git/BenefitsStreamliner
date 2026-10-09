using System.Collections.Concurrent;

namespace BenefitsStreamliner.Api.Services;

public class RetryQueue
{
    private const int MaxAttempts = 3;
    private readonly ConcurrentQueue<string> _queue = new();
    private readonly ConcurrentDictionary<string, int> _attempts = new();

    /// Returns false when max attempts were exceeded (caller should mark the application as Error).
    public bool Enqueue(string applicationId)
    {
        var n = _attempts.AddOrUpdate(applicationId, 1, (_, c) => c + 1);
        if (n > MaxAttempts) return false;
        _queue.Enqueue(applicationId);
        return true;
    }

    public bool TryDequeue(out string applicationId) => _queue.TryDequeue(out applicationId!);
    public int Count => _queue.Count;
}

public class RetryWorker(RetryQueue queue, IServiceScopeFactory scopes, ILogger<RetryWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(TimeSpan.FromSeconds(15), ct); }
            catch (OperationCanceledException) { break; }

            var pending = queue.Count;   // snapshot so re-queued items wait for the next cycle
            for (var i = 0; i < pending && queue.TryDequeue(out var id); i++)
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    var svc = scope.ServiceProvider.GetRequiredService<BenefitsCheckService>();
                    var result = await svc.CheckAsync(id, ct);
                    logger.LogInformation("Retry for {Id} finished with status {Status}", id, result?.Status);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Retry processing failed for {Id}", id);
                }
            }
        }
    }
}