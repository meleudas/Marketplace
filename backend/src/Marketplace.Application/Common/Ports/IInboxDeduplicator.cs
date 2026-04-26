namespace Marketplace.Application.Common.Ports;

public interface IInboxDeduplicator
{
    Task<bool> HasProcessedAsync(Guid messageId, string consumer, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid messageId, string consumer, string? metadata, CancellationToken ct = default);
}
