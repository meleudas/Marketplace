using System.Text.Json;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Coupons;

public static class CouponApplicableScope
{
    public static bool IsUnrestricted(JsonBlob? blob) => blob is null || blob.IsEmpty;

    public static bool ContainsGuid(JsonBlob? blob, Guid value)
    {
        if (IsUnrestricted(blob))
            return true;

        foreach (var token in ParseTokens(blob!))
        {
            if (Guid.TryParse(token, out var g) && g == value)
                return true;
        }

        return false;
    }

    public static bool ContainsLong(JsonBlob? blob, long value)
    {
        if (IsUnrestricted(blob))
            return true;

        foreach (var token in ParseTokens(blob!))
        {
            if (long.TryParse(token, out var n) && n == value)
                return true;
        }

        return false;
    }

    public static bool IsValidJsonArray(JsonBlob? blob)
    {
        if (IsUnrestricted(blob))
            return true;

        try
        {
            using var doc = JsonDocument.Parse(blob!.Raw ?? "[]");
            return doc.RootElement.ValueKind == JsonValueKind.Array;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> ParseTokens(JsonBlob blob)
    {
        if (string.IsNullOrWhiteSpace(blob.Raw))
            return [];

        return ParseTokensCore(blob.Raw);
    }

    private static IReadOnlyList<string> ParseTokensCore(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return [];

            var result = new List<string>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var token = el.ValueKind switch
                {
                    JsonValueKind.String => el.GetString(),
                    JsonValueKind.Number => el.GetRawText(),
                    _ => null
                };
                if (!string.IsNullOrWhiteSpace(token))
                    result.Add(token.Trim());
            }

            return result;
        }
        catch
        {
            return [];
        }
    }
}
