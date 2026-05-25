using Marketplace.Application.Common.Ports;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class AppTransactionPort : IAppTransactionPort
{
    private readonly ApplicationDbContext _context;

    public AppTransactionPort(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        await action(ct);
        await tx.CommitAsync(ct);
    }
}
