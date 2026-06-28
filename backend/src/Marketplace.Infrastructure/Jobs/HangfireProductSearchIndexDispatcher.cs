using Hangfire;
using Marketplace.Application.Products.Ports;

namespace Marketplace.Infrastructure.Jobs;

public sealed class HangfireProductSearchIndexDispatcher : IProductSearchIndexDispatcher
{
    private readonly IBackgroundJobClient _jobs;

    public HangfireProductSearchIndexDispatcher(IBackgroundJobClient jobs)
    {
        _jobs = jobs;
    }

    public Task EnqueueUpsertProductAsync(long productId, CancellationToken ct = default)
    {
        _jobs.Enqueue<SearchIndexJobs>(x => x.UpsertProductAsync(productId, default));
        return Task.CompletedTask;
    }

    public Task EnqueueDeleteProductAsync(long productId, CancellationToken ct = default)
    {
        _jobs.Enqueue<SearchIndexJobs>(x => x.DeleteProductAsync(productId, default));
        return Task.CompletedTask;
    }
}
