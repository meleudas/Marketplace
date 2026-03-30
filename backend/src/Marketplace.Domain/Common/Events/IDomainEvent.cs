using System;


namespace Marketplace.Domain.Common.Events
{
    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
    }
}
