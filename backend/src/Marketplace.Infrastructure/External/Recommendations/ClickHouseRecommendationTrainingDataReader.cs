using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using Marketplace.Application.Products.Ports;
using Marketplace.Infrastructure.External.Analytics;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.External.Recommendations;

public sealed class ClickHouseRecommendationTrainingDataReader : IRecommendationTrainingDataReader
{
    private readonly HttpClient _httpClient;
    private readonly ClickHouseOptions _options;

    public ClickHouseRecommendationTrainingDataReader(HttpClient httpClient, IOptions<ClickHouseOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(5, _options.CommandTimeoutSeconds));

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.Username}:{_options.Password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        }
    }

    public async Task<IReadOnlyList<RecommendationTrainingRow>> ReadAsync(
        DateTime sinceUtc,
        int limit,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return [];

        var safeLimit = Math.Max(1, limit);
        var sql = $"""
            SELECT
                user_id,
                product_id,
                toFloat32(signal_score) AS label,
                toInt32(view_count) AS view_count,
                toInt32(search_count) AS search_count,
                toInt32(favorite_count) AS favorite_count,
                toInt32(cart_count) AS cart_count,
                toInt32(purchase_count) AS purchase_count,
                updated_at_utc
            FROM {_options.Database}.analytics_user_item_signals
            WHERE updated_at_utc >= toDateTime64('{sinceUtc.ToUniversalTime():yyyy-MM-dd HH:mm:ss}', 3, 'UTC')
            ORDER BY updated_at_utc DESC
            LIMIT {safeLimit}
            FORMAT TabSeparatedWithNamesAndTypes
            """;

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildSqlQueryUri(sql))
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, "text/plain")
        };

        using var response = await _httpClient.SendAsync(request, ct);
        var content = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"ClickHouse training read failed ({(int)response.StatusCode}): {content}");

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 2)
            return [];

        var rows = new List<RecommendationTrainingRow>(Math.Min(lines.Length, safeLimit));
        for (var i = 2; i < lines.Length; i++)
        {
            var parts = lines[i].Split('\t');
            if (parts.Length < 9)
                continue;

            if (!Guid.TryParse(parts[0], out var userId))
                continue;
            if (!long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var productId))
                continue;
            if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var label))
                continue;
            _ = int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var viewCount);
            _ = int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var searchCount);
            _ = int.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var favoriteCount);
            _ = int.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out var cartCount);
            _ = int.TryParse(parts[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out var purchaseCount);
            _ = DateTime.TryParse(parts[8], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var updatedAtUtc);

            rows.Add(new RecommendationTrainingRow(
                userId,
                productId,
                label,
                viewCount,
                searchCount,
                favoriteCount,
                cartCount,
                purchaseCount,
                updatedAtUtc == default ? DateTime.UtcNow : updatedAtUtc));
        }

        return rows;
    }

    private string BuildSqlQueryUri(string sql) =>
        $"?database={Uri.EscapeDataString(_options.Database)}&query={Uri.EscapeDataString(sql)}";
}
