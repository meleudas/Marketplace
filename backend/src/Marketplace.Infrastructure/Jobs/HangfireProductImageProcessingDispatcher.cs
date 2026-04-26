using Hangfire;
using Marketplace.Application.Products.Ports;

namespace Marketplace.Infrastructure.Jobs;

public sealed class HangfireProductImageProcessingDispatcher : IProductImageProcessingDispatcher
{
    private readonly IBackgroundJobClient _jobs;

    public HangfireProductImageProcessingDispatcher(IBackgroundJobClient jobs)
    {
        _jobs = jobs;
    }

    public Task EnqueueDerivativesAsync(long productId, string originalObjectKey, CancellationToken ct = default)
    {
        _jobs.Enqueue<ProductImageJobs>(x => x.ProcessDerivativesAsync(productId, originalObjectKey, default));
        return Task.CompletedTask;
    }
}
