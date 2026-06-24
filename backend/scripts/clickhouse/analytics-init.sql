CREATE DATABASE IF NOT EXISTS marketplace;

CREATE TABLE IF NOT EXISTS marketplace.analytics_events
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
ORDER BY (event_type, user_id, session_id, occurred_at_utc, event_id);

CREATE TABLE IF NOT EXISTS marketplace.analytics_user_item_signals
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
ORDER BY (snapshot_date, user_id, product_id);

CREATE TABLE IF NOT EXISTS marketplace.analytics_funnel_daily
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
ORDER BY day;
