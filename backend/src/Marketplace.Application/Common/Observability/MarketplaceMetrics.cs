using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Marketplace.Application.Common.Observability;

public static class MarketplaceMetrics
{
    public const string MeterName = "Marketplace.Observability";
    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> CacheHits = Meter.CreateCounter<long>("cache_hits_total");
    public static readonly Counter<long> CacheMisses = Meter.CreateCounter<long>("cache_misses_total");
    public static readonly Counter<long> CacheErrors = Meter.CreateCounter<long>("cache_errors_total");
    public static readonly Histogram<double> CacheLatencyMs = Meter.CreateHistogram<double>("cache_latency_ms");

    public static readonly Counter<long> PaymentOps = Meter.CreateCounter<long>("payment_operations_total");
    public static readonly Counter<long> PaymentErrors = Meter.CreateCounter<long>("payment_errors_total");
    public static readonly Histogram<double> PaymentLatencyMs = Meter.CreateHistogram<double>("payment_latency_ms");

    public static readonly Counter<long> WebhookOps = Meter.CreateCounter<long>("webhook_operations_total");
    public static readonly Counter<long> WebhookErrors = Meter.CreateCounter<long>("webhook_errors_total");
    public static readonly Histogram<double> WebhookLatencyMs = Meter.CreateHistogram<double>("webhook_latency_ms");

    public static readonly Counter<long> HangfireJobs = Meter.CreateCounter<long>("hangfire_jobs_total");
    public static readonly Counter<long> HangfireJobErrors = Meter.CreateCounter<long>("hangfire_job_errors_total");
    public static readonly Histogram<double> HangfireJobLatencyMs = Meter.CreateHistogram<double>("hangfire_job_latency_ms");

    public static readonly Counter<long> CartOps = Meter.CreateCounter<long>("cart_operations_total");
    public static readonly Counter<long> CartErrors = Meter.CreateCounter<long>("cart_errors_total");
    public static readonly Histogram<double> CartLatencyMs = Meter.CreateHistogram<double>("cart_latency_ms");

    public static readonly Counter<long> CheckoutOps = Meter.CreateCounter<long>("checkout_operations_total");
    public static readonly Counter<long> CheckoutErrors = Meter.CreateCounter<long>("checkout_errors_total");
    public static readonly Histogram<double> CheckoutLatencyMs = Meter.CreateHistogram<double>("checkout_latency_ms");

    public static readonly Counter<long> CatalogOps = Meter.CreateCounter<long>("catalog_operations_total");
    public static readonly Counter<long> CatalogErrors = Meter.CreateCounter<long>("catalog_errors_total");
    public static readonly Histogram<double> CatalogLatencyMs = Meter.CreateHistogram<double>("catalog_latency_ms");
    public static readonly Counter<long> CatalogSearchFallbacks = Meter.CreateCounter<long>("catalog_search_fallback_total");
    public static readonly Counter<long> CatalogSimilarProductsFallbacks = Meter.CreateCounter<long>("catalog_similar_products_fallback_total");
    public static readonly Counter<long> OrderCacheInvalidationFailures = Meter.CreateCounter<long>("order_cache_invalidation_failures_total");

    public static readonly Counter<long> CompanyOps = Meter.CreateCounter<long>("company_operations_total");
    public static readonly Counter<long> CompanyErrors = Meter.CreateCounter<long>("company_errors_total");
    public static readonly Histogram<double> CompanyLatencyMs = Meter.CreateHistogram<double>("company_latency_ms");

    public static readonly Counter<long> FavoriteOps = Meter.CreateCounter<long>("favorites_operations_total");
    public static readonly Counter<long> FavoriteErrors = Meter.CreateCounter<long>("favorites_errors_total");
    public static readonly Histogram<double> FavoriteLatencyMs = Meter.CreateHistogram<double>("favorites_latency_ms");

    public static readonly Counter<long> AuthOps = Meter.CreateCounter<long>("auth_operations_total");
    public static readonly Counter<long> AuthErrors = Meter.CreateCounter<long>("auth_errors_total");
    public static readonly Histogram<double> AuthLatencyMs = Meter.CreateHistogram<double>("auth_latency_ms");

    public static readonly Counter<long> OrderOps = Meter.CreateCounter<long>("order_operations_total");
    public static readonly Counter<long> OrderErrors = Meter.CreateCounter<long>("order_errors_total");
    public static readonly Histogram<double> OrderLatencyMs = Meter.CreateHistogram<double>("order_latency_ms");

    public static readonly Counter<long> ProductOps = Meter.CreateCounter<long>("product_operations_total");
    public static readonly Counter<long> ProductErrors = Meter.CreateCounter<long>("product_errors_total");
    public static readonly Histogram<double> ProductLatencyMs = Meter.CreateHistogram<double>("product_latency_ms");

    public static readonly Counter<long> ReviewOps = Meter.CreateCounter<long>("review_operations_total");
    public static readonly Counter<long> ReviewErrors = Meter.CreateCounter<long>("review_errors_total");
    public static readonly Histogram<double> ReviewLatencyMs = Meter.CreateHistogram<double>("review_latency_ms");

    public static readonly Counter<long> ShippingOps = Meter.CreateCounter<long>("shipping_operations_total");
    public static readonly Counter<long> ShippingErrors = Meter.CreateCounter<long>("shipping_errors_total");
    public static readonly Histogram<double> ShippingLatencyMs = Meter.CreateHistogram<double>("shipping_latency_ms");
    public static readonly Counter<long> ShipmentCreated = Meter.CreateCounter<long>("shipment_created_total");
    public static readonly Counter<long> ShipmentDeliveryStatus = Meter.CreateCounter<long>("shipment_delivery_status_total");
    public static readonly Counter<long> ShippingWebhookEvents = Meter.CreateCounter<long>("shipping_webhook_events_total");
    public static readonly Counter<long> RateLimitRejected = Meter.CreateCounter<long>("rate_limit_rejected_total");
    public static readonly Counter<long> AbuseRejected = Meter.CreateCounter<long>("abuse_rejected_total");
    public static readonly Counter<long> CouponOps = Meter.CreateCounter<long>("coupon_operations_total");
    public static readonly Counter<long> CouponErrors = Meter.CreateCounter<long>("coupon_errors_total");
    public static readonly Counter<long> CouponValidationFailures = Meter.CreateCounter<long>("coupon_validation_failures_total");
    public static readonly Counter<long> ReportOps = Meter.CreateCounter<long>("report_operations_total");
    public static readonly Counter<long> ReportErrors = Meter.CreateCounter<long>("report_errors_total");
    public static readonly Counter<long> ReportSlaBreaches = Meter.CreateCounter<long>("report_sla_breach_total");
    public static readonly UpDownCounter<int> ReportQueueBacklog = Meter.CreateUpDownCounter<int>("report_queue_backlog");
    public static readonly Histogram<double> ReportResolutionLatencyMs = Meter.CreateHistogram<double>("report_resolution_latency_ms");

    public static readonly Counter<long> ChatMessagesTotal = Meter.CreateCounter<long>("chat_messages_total");
    public static readonly Counter<long> ChatMessageErrorsTotal = Meter.CreateCounter<long>("chat_message_errors_total");
    public static readonly Histogram<double> ChatMessageLatencyMs = Meter.CreateHistogram<double>("chat_message_latency_ms");
    public static readonly Counter<long> ChatSpamBlockTotal = Meter.CreateCounter<long>("chat_spam_block_total");
    public static readonly UpDownCounter<int> ChatUnreadBacklog = Meter.CreateUpDownCounter<int>("chat_unread_backlog");

    public static readonly Counter<long> SupportTicketsTotal = Meter.CreateCounter<long>("support_tickets_total");
    public static readonly Counter<long> SupportTicketErrorsTotal = Meter.CreateCounter<long>("support_ticket_errors_total");
    public static readonly Histogram<double> SupportTicketLatencyMs = Meter.CreateHistogram<double>("support_ticket_latency_ms");
    public static readonly Counter<long> SupportSlaBreachTotal = Meter.CreateCounter<long>("support_sla_breach_total");
    public static readonly Counter<long> SupportHelpdeskSyncFailuresTotal = Meter.CreateCounter<long>("support_helpdesk_sync_failures_total");

    public static readonly Counter<long> NotificationDispatches = Meter.CreateCounter<long>("notification_dispatch_total");
    public static readonly Counter<long> NotificationDispatchErrors = Meter.CreateCounter<long>("notification_dispatch_errors_total");
    public static readonly Histogram<double> NotificationDispatchLatencyMs = Meter.CreateHistogram<double>("notification_dispatch_latency_ms");
    public static readonly Counter<long> NotificationChannelDeliveries = Meter.CreateCounter<long>("notification_channel_deliveries_total");
    public static readonly Counter<long> NotificationChannelErrors = Meter.CreateCounter<long>("notification_channel_errors_total");
    public static readonly Counter<long> NotificationDispatchDeadLetter = Meter.CreateCounter<long>("notification_dispatch_dead_letter_total");

    public static readonly Counter<long> OutboxDispatches = Meter.CreateCounter<long>("outbox_dispatch_total");
    public static readonly Counter<long> OutboxDispatchErrors = Meter.CreateCounter<long>("outbox_dispatch_errors_total");
    public static readonly Counter<long> OutboxDeadLetters = Meter.CreateCounter<long>("outbox_dead_letter_total");
    public static readonly Counter<long> IntegrationRetryAttempts = Meter.CreateCounter<long>("integration_retry_attempts_total");
    public static readonly Counter<long> IntegrationRetryDeadLetters = Meter.CreateCounter<long>("integration_retry_dead_letter_total");
    public static readonly Counter<long> IdempotencyBegins = Meter.CreateCounter<long>("idempotency_begin_total");
    public static readonly Counter<long> IdempotencyConflicts = Meter.CreateCounter<long>("idempotency_conflicts_total");
    public static readonly Counter<long> IdempotencyReplays = Meter.CreateCounter<long>("idempotency_replays_total");
    public static readonly Counter<long> AnalyticsEventsIngested = Meter.CreateCounter<long>("analytics_events_ingested_total");
    public static readonly Counter<long> AnalyticsEventsDropped = Meter.CreateCounter<long>("analytics_events_dropped_total");
    public static readonly Histogram<double> AnalyticsPipelineLatencyMs = Meter.CreateHistogram<double>("analytics_pipeline_latency_ms");
    public static readonly Counter<long> AnalyticsAggregationFailures = Meter.CreateCounter<long>("analytics_aggregation_failures_total");
    public static readonly Counter<long> RecommendationModelTrainings = Meter.CreateCounter<long>("recommendation_model_trainings_total");
    public static readonly Counter<long> RecommendationModelPromotions = Meter.CreateCounter<long>("recommendation_model_promotions_total");
    public static readonly Counter<long> RecommendationFallbacks = Meter.CreateCounter<long>("recommendation_fallbacks_total");
    public static readonly Histogram<double> RecommendationInferenceLatencyMs = Meter.CreateHistogram<double>("recommendation_inference_latency_ms");

    public static readonly Counter<long> CommissionPosted = Meter.CreateCounter<long>("commission_posted_total");
    public static readonly Counter<long> SellerLedgerEntries = Meter.CreateCounter<long>("seller_ledger_entries_total");
    public static readonly Counter<long> SellerPayoutTotal = Meter.CreateCounter<long>("seller_payout_total");
    public static readonly Counter<long> SettlementBatchTotal = Meter.CreateCounter<long>("settlement_batch_total");

    public static IDisposable StartTimer(Histogram<double> histogram, params KeyValuePair<string, object?>[] tags)
        => new TimerScope(histogram, tags);

    private sealed class TimerScope : IDisposable
    {
        private readonly Histogram<double> _histogram;
        private readonly KeyValuePair<string, object?>[] _tags;
        private readonly Stopwatch _sw = Stopwatch.StartNew();

        public TimerScope(Histogram<double> histogram, KeyValuePair<string, object?>[] tags)
        {
            _histogram = histogram;
            _tags = tags;
        }

        public void Dispose()
        {
            _sw.Stop();
            _histogram.Record(_sw.Elapsed.TotalMilliseconds, _tags);
        }
    }
}
