namespace Marketplace.Application.Common.Ports;

public interface IAppTransactionPort
{
    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
}
