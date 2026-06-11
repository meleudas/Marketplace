using Marketplace.Application.Common.Ports;

namespace Marketplace.Application.Support.Ports;

public interface ISupportHelpdeskSyncHandler
{
    Task ProcessAsync(OutboxMessage message, CancellationToken ct = default);
}
