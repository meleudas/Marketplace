using System.Text.Json;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Ports;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Application.Products.Catalog;

public sealed class CatalogFacetAggregator
{
    public CatalogProductFacetsDto Aggregate(IReadOnlyList<ProductFacetSourceRow> sources)
    {
        var authors = new Dictionary<string, FacetAccumulator>(StringComparer.OrdinalIgnoreCase);
        var genres = new Dictionary<string, FacetAccumulator>(StringComparer.OrdinalIgnoreCase);
        var formats = new Dictionary<string, FacetAccumulator>(StringComparer.OrdinalIgnoreCase);
        var tags = new Dictionary<string, FacetAccumulator>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in sources)
        {
            var attributes = new JsonBlob(source.AttributesRaw ?? string.Empty);
            var facets = ProductCatalogFacetReader.Read(attributes, source.Tags, source.Brands);

            foreach (var author in facets.AuthorValues)
                Increment(authors, author, ResolveAuthorLabel(source, author));

            foreach (var genre in facets.Genres)
                Increment(genres, genre, ResolveGenreLabel(source, genre));

            if (!string.IsNullOrWhiteSpace(facets.Format))
                Increment(formats, facets.Format, ProductCatalogFormats.GetLabel(facets.Format));

            foreach (var tag in facets.Tags)
                Increment(tags, tag, tag);
        }

        return new CatalogProductFacetsDto(
            ToOptions(authors),
            ToOptions(genres),
            ToOptions(formats),
            ToOptions(tags));
    }

    private static void Increment(
        Dictionary<string, FacetAccumulator> bucket,
        string normalizedValue,
        string label)
    {
        if (string.IsNullOrWhiteSpace(normalizedValue))
            return;

        if (!bucket.TryGetValue(normalizedValue, out var accumulator))
        {
            bucket[normalizedValue] = new FacetAccumulator(
                normalizedValue,
                string.IsNullOrWhiteSpace(label) ? normalizedValue : label.Trim(),
                1);
            return;
        }

        bucket[normalizedValue] = accumulator with { Count = accumulator.Count + 1 };
    }

    private static IReadOnlyList<CatalogFacetOptionDto> ToOptions(Dictionary<string, FacetAccumulator> bucket) =>
        bucket.Values
            .OrderBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(x => x.Count)
            .Select(x => new CatalogFacetOptionDto(x.Value, x.Label, x.Count))
            .ToArray();

    private static string ResolveAuthorLabel(ProductFacetSourceRow source, string normalizedAuthor)
    {
        foreach (var candidate in EnumerateAuthorCandidates(source))
        {
            if (string.Equals(ProductCatalogFacetReader.Normalize(candidate), normalizedAuthor, StringComparison.OrdinalIgnoreCase))
                return candidate.Trim();
        }

        return normalizedAuthor;
    }

    private static string ResolveGenreLabel(ProductFacetSourceRow source, string normalizedGenre)
    {
        foreach (var candidate in EnumerateGenreCandidates(source))
        {
            if (string.Equals(ProductCatalogFacetReader.Normalize(candidate), normalizedGenre, StringComparison.OrdinalIgnoreCase))
                return candidate.Trim();
        }

        return normalizedGenre;
    }

    private static IEnumerable<string> EnumerateAuthorCandidates(ProductFacetSourceRow source)
    {
        foreach (var value in ReadStringProperty(source.AttributesRaw, "author", "Author", "автор"))
            yield return value;

        foreach (var brand in source.Brands)
        {
            if (!string.IsNullOrWhiteSpace(brand))
                yield return brand;
        }

        foreach (var tag in source.Tags)
        {
            if (TryParsePrefixedTag(tag, "author:", out var author))
                yield return author;
        }
    }

    private static IEnumerable<string> EnumerateGenreCandidates(ProductFacetSourceRow source)
    {
        foreach (var value in ReadStringListProperty(source.AttributesRaw, "genre", "genres", "Genre", "Genres", "жанр"))
            yield return value;

        foreach (var tag in source.Tags)
        {
            if (TryParsePrefixedTag(tag, "genre:", out var genre))
                yield return genre;
            else if (!string.IsNullOrWhiteSpace(tag) && !tag.Contains(':', StringComparison.Ordinal))
                yield return tag;
        }
    }

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

    private static IEnumerable<string> ReadStringProperty(string? attributesRaw, params string[] propertyNames)
    {
        if (string.IsNullOrWhiteSpace(attributesRaw))
            return [];

        try
        {
            using var doc = JsonDocument.Parse(attributesRaw);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return [];

            foreach (var name in propertyNames)
            {
                if (!doc.RootElement.TryGetProperty(name, out var value))
                    continue;

                if (value.ValueKind == JsonValueKind.String)
                {
                    var text = value.GetString();
                    return string.IsNullOrWhiteSpace(text) ? [] : [text];
                }

                if (value.ValueKind == JsonValueKind.Number)
                    return [value.GetRawText()];
            }
        }
        catch (JsonException)
        {
            return [];
        }

        return [];
    }

    private static IEnumerable<string> ReadStringListProperty(string? attributesRaw, params string[] propertyNames)
    {
        if (string.IsNullOrWhiteSpace(attributesRaw))
            return [];

        try
        {
            using var doc = JsonDocument.Parse(attributesRaw);
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

    private sealed record FacetAccumulator(string Value, string Label, int Count);
}
