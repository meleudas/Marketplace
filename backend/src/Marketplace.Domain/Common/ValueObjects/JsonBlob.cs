using Marketplace.Domain.Common.Models;
using System.Collections.Generic;

namespace Marketplace.Domain.Common.ValueObjects;

/// <summary>Представлення JSONB/JSON поля на рівні домену (серіалізація — у Infrastructure).</summary>
public sealed record JsonBlob : ValueObject
{
    public string? Raw { get; }

    public JsonBlob(string? raw)
    {
        Raw = raw;
    }

    public static JsonBlob Empty => new(raw: null);

    public bool IsEmpty => string.IsNullOrWhiteSpace(Raw);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Raw ?? string.Empty;
    }
}
