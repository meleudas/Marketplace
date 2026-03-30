using Marketplace.Domain.Common.Events;
using Marketplace.Domain.Users.ValueObjects;

namespace Marketplace.Domain.Users.Events;

public record UserEmailVerifiedEvent(UserId UserId, Email Email) : DomainEventBase;
