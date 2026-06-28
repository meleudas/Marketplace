using Marketplace.Application.Common.Ports;

namespace Marketplace.Tests.Common.Fakes;

public static class OutboxWriterFakeDefaults
{
    public static Task<(IReadOnlyList<OutboxMessage> Items, long Total)> EmptyListAsync(
        int page,
        int pageSize,
        CancellationToken ct = default)
        => Task.FromResult(((IReadOnlyList<OutboxMessage>)Array.Empty<OutboxMessage>(), 0L));
}
