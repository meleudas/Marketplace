using Marketplace.Domain.Common.Events;
using Marketplace.Domain.Users.ValueObjects;

namespace Marketplace.Domain.Users.Events;

/// <summary>
/// Email змінено — потрібна повторна верифікація (див. <see cref="Entities.User.UpdateEmail"/>).
/// </summary>
public record UserEmailChangedEvent(UserId UserId, Email NewEmail) : DomainEventBase;
