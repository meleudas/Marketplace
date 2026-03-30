using Marketplace.Domain.Common.Events;
using System.Collections.Generic;
namespace Marketplace.Domain.Common.Models;

/// <summary>
/// Базовий тип сутності з доменними подіями. Ідентифікатор задає <see cref="AggregateRoot{TId}"/>.
/// </summary>
public abstract class Entity
{
    protected Entity() { }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
