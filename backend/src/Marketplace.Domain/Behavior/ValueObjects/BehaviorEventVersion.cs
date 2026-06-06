using Marketplace.Domain.Common.Models;
using System.Collections.Generic;

namespace Marketplace.Domain.Behavior.ValueObjects;

public sealed record BehaviorEventVersion : ValueObject
{
    public short Value { get; }

    private BehaviorEventVersion(short value)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), "event_version must be positive.");
        Value = value;
    }

    public static BehaviorEventVersion V1 => new(1);

    public static BehaviorEventVersion From(short value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
