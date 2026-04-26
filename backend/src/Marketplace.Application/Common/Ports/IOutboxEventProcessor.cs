namespace Marketplace.Application.Common.Ports;

public interface IOutboxEventProcessor
{
    Task ProcessAsync(OutboxMessage message, CancellationToken ct = default);
}
