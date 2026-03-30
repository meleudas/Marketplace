using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Domain.Common.Events
{
    public abstract record DomainEventBase : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
