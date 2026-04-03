using Marketplace.Domain.Common.Models;
using System.Collections.Generic;

namespace Marketplace.Domain.Common.ValueObjects;

/// <summary>Грошова сума (узгоджено з numeric(14,2) у PostgreSQL).</summary>
public sealed record Money : ValueObject
{
    public decimal Amount { get; }

    public Money(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    public static Money Zero => new(0);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
    }
}
