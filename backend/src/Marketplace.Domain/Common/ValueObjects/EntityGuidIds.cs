using Marketplace.Domain.Common.Models;
using System.Collections.Generic;

namespace Marketplace.Domain.Common.ValueObjects;

/// <summary>Ідентифікатори uuid PK (наприклад, чати).</summary>
public sealed record ChatId : ValueObject
{
    public Guid Value { get; }
    private ChatId(Guid value) => Value = value;
    public static ChatId From(Guid value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}
