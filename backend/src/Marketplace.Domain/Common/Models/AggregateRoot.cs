namespace Marketplace.Domain.Common.Models;

public abstract class AggregateRoot<TId> : Entity
{
    public TId Id { get; protected set; } = default!;
}
