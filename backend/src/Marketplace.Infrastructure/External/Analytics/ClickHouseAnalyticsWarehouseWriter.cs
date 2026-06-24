using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Marketplace.Application.Behavior.Ports;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.External.Analytics;

public sealed class ClickHouseAnalyticsWarehouseWriter : IAnalyticsWarehouseWriter
{
    private readonly HttpClient _httpClient;
    private readonly ClickHouseOptions _options;
    private readonly SemaphoreSlim _schemaLock = new(1, 1);
    private volatile bool _schemaInitialized;

    public ClickHouseAnalyticsWarehouseWriter(HttpClient httpClient, IOptions<ClickHouseOptions> options)
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

    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled || _schemaInitialized)
            return;

        await _schemaLock.WaitAsync(ct);
        try
        {
            if (_schemaInitialized)
                return;

            await ExecuteAsync($"CREATE DATABASE IF NOT EXISTS {_options.Database}", ct);
            await ExecuteAsync($"""
                CREATE TABLE IF NOT EXISTS {_options.Database}.analytics_events
                (
                    event_id UUID,
                    event_type LowCardinality(String),
                    occurred_at_utc DateTime64(3, 'UTC'),
                    user_id Nullable(UUID),
                    session_id String,
                    product_id Nullable(Int64),
                    query Nullable(String),
                    source LowCardinality(String),
                    schema_version UInt16,
                    payload_json String,
                    created_at_utc DateTime64(3, 'UTC')
                )
                ENGINE = MergeTree
                PARTITION BY toDate(occurred_at_utc)
                ORDER BY (event_type, user_id, session_id, occurred_at_utc, event_id)
                """, ct);

            await ExecuteAsync($"""
                CREATE TABLE IF NOT EXISTS {_options.Database}.analytics_user_item_signals
                (
                    snapshot_date Date,
                    user_id UUID,
                    product_id Int64,
                    signal_score Float64,
                    view_count UInt32,
                    search_count UInt32,
                    favorite_count UInt32,
                    cart_count UInt32,
                    purchase_count UInt32,
                    updated_at_utc DateTime64(3, 'UTC')
                )
                ENGINE = ReplacingMergeTree(updated_at_utc)
                PARTITION BY snapshot_date
                ORDER BY (snapshot_date, user_id, product_id)
                """, ct);

            await ExecuteAsync($"""
                CREATE TABLE IF NOT EXISTS {_options.Database}.analytics_funnel_daily
                (
                    day Date,
                    product_views UInt64,
                    search_queries UInt64,
                    favorite_adds UInt64,
                    cart_adds UInt64,
                    purchases UInt64,
                    updated_at_utc DateTime64(3, 'UTC')
                )
                ENGINE = ReplacingMergeTree(updated_at_utc)
                PARTITION BY day
                ORDER BY day
                """, ct);

            _schemaInitialized = true;
        }
        finally
        {
            _schemaLock.Release();
        }
    }

    public async Task WriteEventAsync(AnalyticsWarehouseEvent evt, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return;

        await EnsureSchemaAsync(ct);
        var row = JsonSerializer.Serialize(new
        {
            event_id = evt.EventId,
            event_type = evt.EventType,
            occurred_at_utc = evt.OccurredAtUtc.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
            user_id = evt.UserId,
            session_id = evt.SessionId,
            product_id = evt.ProductId,
            query = evt.Query,
            source = evt.Source,
            schema_version = evt.SchemaVersion,
            payload_json = evt.PayloadJson,
            created_at_utc = evt.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)
        });

        await ExecuteAsync(
            $"INSERT INTO {_options.Database}.analytics_events FORMAT JSONEachRow",
            ct,
            row + "\n");
    }

    public async Task RebuildUserItemSignalsAsync(int lookbackDays, int halfLifeDays, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return;

        await EnsureSchemaAsync(ct);
        var safeLookback = Math.Max(1, lookbackDays);
        var safeHalfLife = Math.Max(1, halfLifeDays);

        await ExecuteAsync($"TRUNCATE TABLE {_options.Database}.analytics_user_item_signals", ct);
        await ExecuteAsync($"""
            INSERT INTO {_options.Database}.analytics_user_item_signals
            SELECT
                today() AS snapshot_date,
                assumeNotNull(user_id) AS user_id,
                product_id AS product_id,
                sum(weight * decay) AS signal_score,
                sum(if(event_type = 'ProductView', 1, 0)) AS view_count,
                sum(if(event_type = 'SearchQuery', 1, 0)) AS search_count,
                sum(if(event_type = 'FavoriteAdd', 1, 0)) AS favorite_count,
                sum(if(event_type = 'AddToCart' OR event_type = 'CartAdd', 1, 0)) AS cart_count,
                sum(if(event_type = 'PurchaseCompleted', 1, 0)) AS purchase_count,
                now64(3) AS updated_at_utc
            FROM
            (
                SELECT
                    user_id,
                    product_id,
                    event_type,
                    multiIf(
                        event_type = 'ProductView', {_options.ViewWeight.ToString(CultureInfo.InvariantCulture)},
                        event_type = 'SearchQuery', {_options.SearchWeight.ToString(CultureInfo.InvariantCulture)},
                        event_type = 'FavoriteAdd', {_options.FavoriteWeight.ToString(CultureInfo.InvariantCulture)},
                        event_type = 'AddToCart' OR event_type = 'CartAdd', {_options.CartWeight.ToString(CultureInfo.InvariantCulture)},
                        event_type = 'PurchaseCompleted', {_options.PurchaseWeight.ToString(CultureInfo.InvariantCulture)},
                        0.0
                    ) AS weight,
                    exp(-toFloat64(dateDiff('day', toDate(occurred_at_utc), today())) / {safeHalfLife}) AS decay
                FROM {_options.Database}.analytics_events
                WHERE user_id IS NOT NULL
                  AND product_id IS NOT NULL
                  AND occurred_at_utc >= now() - INTERVAL {safeLookback} DAY
            )
            GROUP BY user_id, product_id
            HAVING signal_score > 0
            """, ct);
    }

    public async Task RebuildFunnelDailyAsync(int lookbackDays, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return;

        await EnsureSchemaAsync(ct);
        var safeLookback = Math.Max(1, lookbackDays);

        await ExecuteAsync($"TRUNCATE TABLE {_options.Database}.analytics_funnel_daily", ct);
        await ExecuteAsync($"""
            INSERT INTO {_options.Database}.analytics_funnel_daily
            SELECT
                toDate(occurred_at_utc) AS day,
                countIf(event_type = 'ProductView') AS product_views,
                countIf(event_type = 'SearchQuery') AS search_queries,
                countIf(event_type = 'FavoriteAdd') AS favorite_adds,
                countIf(event_type = 'AddToCart' OR event_type = 'CartAdd') AS cart_adds,
                countIf(event_type = 'PurchaseCompleted') AS purchases,
                now64(3) AS updated_at_utc
            FROM {_options.Database}.analytics_events
            WHERE occurred_at_utc >= now() - INTERVAL {safeLookback} DAY
            GROUP BY day
            ORDER BY day
            """, ct);
    }

    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return true;

        try
        {
            await ExecuteAsync("SELECT 1", ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task ExecuteAsync(string sql, CancellationToken ct, string? body = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildSqlQueryUri(sql));
        request.Content = new StringContent(body ?? string.Empty, Encoding.UTF8, "text/plain");
        using var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"ClickHouse query failed ({(int)response.StatusCode}): {responseBody}");
    }

    private string BuildSqlQueryUri(string sql) =>
        $"?database={Uri.EscapeDataString(_options.Database)}&query={Uri.EscapeDataString(sql)}";
}
