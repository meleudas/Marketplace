using System.Text.Json;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Application.Products.Catalog;

public static class ProductCatalogFacetReader
{
    public static ProductCatalogFacets Read(
        JsonBlob attributes,
        IReadOnlyList<string>? tags,
        IReadOnlyList<string>? brands = null)
    {
        var tagList = tags ?? [];
        var authorCandidates = new List<string>();
        var formatFromTags = new List<string>();
        var genres = new List<string>();
        var normalizedTags = new List<string>();

        foreach (var tag in tagList)
        {
            if (string.IsNullOrWhiteSpace(tag))
                continue;

            if (TryParsePrefixedTag(tag, "genre:", out var genreFromTag))
                genres.Add(Normalize(genreFromTag));
            else if (TryParsePrefixedTag(tag, "format:", out var formatFromTag))
                formatFromTags.Add(Normalize(formatFromTag));
            else if (TryParsePrefixedTag(tag, "author:", out var authorFromTag))
                authorCandidates.Add(Normalize(authorFromTag));
            else
                normalizedTags.Add(Normalize(tag));
        }

        var authorFromAttributes = ReadStringProperty(attributes, "author", "Author", "автор");
        if (!string.IsNullOrWhiteSpace(authorFromAttributes))
            authorCandidates.Insert(0, Normalize(authorFromAttributes));

        foreach (var brand in brands ?? [])
        {
            if (!string.IsNullOrWhiteSpace(brand))
                authorCandidates.Add(Normalize(brand));
        }

        var authorValues = authorCandidates
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var format = ReadStringProperty(attributes, "format", "Format", "формат")
            ?? formatFromTags.FirstOrDefault();
        genres.AddRange(ReadStringListProperty(attributes, "genre", "genres", "Genre", "Genres", "жанр")
            .Select(Normalize));

        return new ProductCatalogFacets(
            authorValues.Length > 0 ? authorValues[0] : null,
            authorValues,
            string.IsNullOrWhiteSpace(format) ? null : Normalize(format),
            genres.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            normalizedTags.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    public static bool Matches(
        ProductCatalogFacets facets,
        IReadOnlyList<string>? authors,
        string? format,
        IReadOnlyList<string>? genres,
        IReadOnlyList<string>? tags)
    {
        if (!MatchesAnyAuthor(facets, authors))
            return false;

        if (!string.IsNullOrWhiteSpace(format) &&
            !string.Equals(facets.Format, Normalize(format), StringComparison.OrdinalIgnoreCase))
            return false;

        if (!MatchesAnyGenre(facets, genres))
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

    public static bool HasFacetFilters(
        IReadOnlyList<string>? authors,
        string? format,
        IReadOnlyList<string>? genres,
        IReadOnlyList<string>? tags) =>
        authors is { Count: > 0 }
        || !string.IsNullOrWhiteSpace(format)
        || genres is { Count: > 0 }
        || tags is { Count: > 0 };

    public static IReadOnlyList<string> NormalizeAuthors(IReadOnlyList<string>? authors)
    {
        if (authors is null || authors.Count == 0)
            return [];

        return authors
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => Normalize(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<string> NormalizeGenres(IReadOnlyList<string>? genres)
    {
        if (genres is null || genres.Count == 0)
            return [];

        return genres
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => Normalize(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static bool MatchesAnyAuthor(ProductCatalogFacets facets, IReadOnlyList<string>? authors)
    {
        var normalizedAuthors = NormalizeAuthors(authors);
        if (normalizedAuthors.Count == 0)
            return true;

        if (facets.AuthorValues.Count == 0)
            return false;

        return normalizedAuthors.Any(requested =>
            facets.AuthorValues.Contains(requested, StringComparer.OrdinalIgnoreCase));
    }

    public static bool MatchesAnyGenre(ProductCatalogFacets facets, IReadOnlyList<string>? genres)
    {
        var normalizedGenres = NormalizeGenres(genres);
        if (normalizedGenres.Count == 0)
            return true;

        foreach (var genre in normalizedGenres)
        {
            if (facets.Genres.Contains(genre, StringComparer.OrdinalIgnoreCase))
                return true;

            if (facets.Tags.Contains(genre, StringComparer.OrdinalIgnoreCase))
                return true;
        }

        return false;
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
