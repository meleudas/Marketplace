using System.Text.Json;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Application.Products.Catalog;

public static class ProductCatalogFacetReader
{
    public static ProductCatalogFacets Read(JsonBlob attributes, IReadOnlyList<string>? tags)
    {
        var tagList = tags ?? [];
        var genres = new List<string>();
        var normalizedTags = new List<string>();

        foreach (var tag in tagList)
        {
            if (string.IsNullOrWhiteSpace(tag))
                continue;

            if (TryParsePrefixedTag(tag, "genre:", out var genreFromTag))
                genres.Add(Normalize(genreFromTag));
            else if (TryParsePrefixedTag(tag, "format:", out _))
                continue;
            else if (TryParsePrefixedTag(tag, "author:", out _))
                continue;
            else
                normalizedTags.Add(Normalize(tag));
        }

        var author = ReadStringProperty(attributes, "author", "Author", "автор");
        var format = ReadStringProperty(attributes, "format", "Format", "формат");
        genres.AddRange(ReadStringListProperty(attributes, "genre", "genres", "Genre", "Genres", "жанр")
            .Select(Normalize));

        return new ProductCatalogFacets(
            string.IsNullOrWhiteSpace(author) ? null : Normalize(author),
            string.IsNullOrWhiteSpace(format) ? null : Normalize(format),
            genres.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            normalizedTags.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    public static bool Matches(
        ProductCatalogFacets facets,
        string? author,
        string? format,
        string? genre,
        IReadOnlyList<string>? tags)
    {
        if (!string.IsNullOrWhiteSpace(author) &&
            !string.Equals(facets.Author, Normalize(author), StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrWhiteSpace(format) &&
            !string.Equals(facets.Format, Normalize(format), StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrWhiteSpace(genre) &&
            !facets.Genres.Contains(Normalize(genre), StringComparer.OrdinalIgnoreCase))
            return false;

        if (tags is { Count: > 0 })
        {
            foreach (var tag in tags.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                if (!facets.Tags.Contains(Normalize(tag), StringComparer.OrdinalIgnoreCase))
                    return false;
            }
        }

        return true;
    }

    public static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static bool TryParsePrefixedTag(string tag, string prefix, out string value)
    {
        if (tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            value = tag[prefix.Length..];
            return !string.IsNullOrWhiteSpace(value);
        }

        value = string.Empty;
        return false;
    }

    private static string? ReadStringProperty(JsonBlob attributes, params string[] propertyNames)
    {
        if (string.IsNullOrWhiteSpace(attributes.Raw))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(attributes.Raw);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            foreach (var name in propertyNames)
            {
                if (!doc.RootElement.TryGetProperty(name, out var value))
                    continue;

                return value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString(),
                    JsonValueKind.Number => value.GetRawText(),
                    _ => null
                };
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static IEnumerable<string> ReadStringListProperty(JsonBlob attributes, params string[] propertyNames)
    {
        if (string.IsNullOrWhiteSpace(attributes.Raw))
            return [];

        try
        {
            using var doc = JsonDocument.Parse(attributes.Raw);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return [];

            foreach (var name in propertyNames)
            {
                if (!doc.RootElement.TryGetProperty(name, out var value))
                    continue;

                return ReadJsonValues(value).ToArray();
            }
        }
        catch (JsonException)
        {
            return [];
        }

        return [];
    }

    private static IEnumerable<string> ReadJsonValues(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                var text = value.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                    yield return text;
                yield break;
            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var arrayText = item.GetString();
                        if (!string.IsNullOrWhiteSpace(arrayText))
                            yield return arrayText;
                    }
                }

                yield break;
        }
    }
}
