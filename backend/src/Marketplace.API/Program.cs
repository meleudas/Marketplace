using Hangfire;
using Marketplace.API.Extensions;
using Marketplace.API.Options;
using Marketplace.Application.Payments.Ports;
using Marketplace.Application.Products.Options;
using Marketplace.Infrastructure.External.Email;
using Marketplace.Infrastructure.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddMarketplaceOpenTelemetryLogging(builder.Configuration);
builder.Services.AddMarketplaceApi(builder.Configuration, builder.Environment);

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:AutoMigrate"))
    await InitializeDevelopmentDatabaseWithRetriesAsync(app.Services, app.Logger);

app.MapOpenApi("/openapi/{documentName}.json");
app.MapScalarApiReference(options =>
{
    options.Title = "Marketplace API";
    options.Theme = ScalarTheme.Mars;
    options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
    options.OpenApiRoutePattern = "/openapi/{documentName}.json";
});
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Marketplace API v1");
    options.RoutePrefix = "swagger";
});

app.UseMarketplaceMiddleware();
if (app.Environment.IsDevelopment())
    app.UseHangfireDashboard("/hangfire");

if (!app.Environment.IsEnvironment("Testing"))
{
    var recommendationOptions = app.Configuration
        .GetSection(RecommendationModelOptions.SectionName)
        .Get<RecommendationModelOptions>() ?? new RecommendationModelOptions();

    RecurringJob.AddOrUpdate<InventoryJobs>(
        "inventory-expire-reservations",
        job => job.ExpireReservationsAsync(default),
        Cron.Minutely);
    RecurringJob.AddOrUpdate<SearchIndexJobs>(
        "search-full-reindex-products",
        job => job.FullReindexAsync(default),
        Cron.Daily(2));
    RecurringJob.AddOrUpdate<PaymentJobs>(
        "payments-sync-pending-liqpay",
        job => job.SyncPendingPaymentsAsync(default),
        Cron.Minutely);
    RecurringJob.AddOrUpdate<OutboxDispatcherJobs>(
        "outbox-dispatch-pending",
        job => job.DispatchPendingAsync(default),
        Cron.Minutely);
    RecurringJob.AddOrUpdate<IntegrationRetryJobs>(
        "integration-retry-dispatch",
        job => job.DispatchDueAsync(default),
        Cron.Minutely);
    RecurringJob.AddOrUpdate<ShippingSyncJobs>(
        "shipping-sync-pending",
        job => job.SyncPendingAsync(default),
        Cron.Hourly);
    RecurringJob.AddOrUpdate<BehaviorAggregationJobs>(
        "behavior-aggregate-daily",
        job => job.AggregateDailyAsync(default),
        Cron.Hourly);
    RecurringJob.AddOrUpdate<BehaviorAggregationJobs>(
        "behavior-prune-raw-retention",
        job => job.PruneRawRetentionAsync(90, default),
        Cron.Daily(4));
    RecurringJob.AddOrUpdate<AnalyticsWarehouseAggregationJobs>(
        "analytics-warehouse-rebuild-signals",
        job => job.RebuildUserItemSignalsAsync(default),
        Cron.Hourly);
    RecurringJob.AddOrUpdate<AnalyticsWarehouseAggregationJobs>(
        "analytics-warehouse-rebuild-funnel",
        job => job.RebuildFunnelDailyAsync(default),
        Cron.Hourly);
    RecurringJob.AddOrUpdate<MediaCleanupJobs>(
        "media-cleanup-orphans",
        job => job.CleanupOrphansAsync(default),
        Cron.Hourly);
    RecurringJob.AddOrUpdate<AppNotificationJobs>(
        "app-notifications-prune-expired-inapp",
        job => job.PruneExpiredInAppNotificationsAsync(default),
        Cron.Daily(3));
    RecurringJob.AddOrUpdate<SupportHelpdeskReconciliationJobs>(
        "support-helpdesk-reconcile",
        job => job.ReconcileAsync(default),
        Cron.Hourly);
    RecurringJob.AddOrUpdate<SettlementBatchJob>(
        "finance-settlement-batch",
        job => job.RunAsync(default),
        Cron.Hourly);
    RecurringJob.AddOrUpdate<SellerPayoutProcessor>(
        "finance-seller-payout",
        job => job.ProcessAsync(default),
        Cron.Hourly);
    if (recommendationOptions.Enabled)
    {
        RecurringJob.AddOrUpdate<RecommendationModelJobs>(
            "recommendation-model-train",
            job => job.TrainAndValidateAsync(default),
            recommendationOptions.RetrainCron);
        RecurringJob.AddOrUpdate<RecommendationModelJobs>(
            "recommendation-model-promote",
            job => job.PromoteCandidateAsync(default),
            recommendationOptions.PromoteCron);
        RecurringJob.AddOrUpdate<RecommendationModelJobs>(
            "recommendation-model-cleanup",
            job => job.PruneOldArtifactsAsync(default),
            recommendationOptions.CleanupCron);
    }
}

app.MapControllers();
app.MapHub<Marketplace.API.Hubs.ChatHub>("/hubs/chat");

var openTelemetryOptions = app.Configuration
    .GetSection(OpenTelemetryOptions.SectionName)
    .Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();
if (openTelemetryOptions.EnableLegacyPrometheusEndpoint)
{
    app.MapPrometheusScrapingEndpoint("/metrics")
        .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("HealthCheck")
    .WithTags("Health");

app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }))
    .WithName("HealthLive")
    .WithTags("Health");

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var postgresUnhealthy = report.Entries.TryGetValue("postgres", out var pg) && pg.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy;
        context.Response.StatusCode = postgresUnhealthy
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status200OK;
        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(
                x => x.Key,
                x => new { status = x.Value.Status.ToString(), description = x.Value.Description })
        };
        await context.Response.WriteAsJsonAsync(payload);
    }
})
    .WithName("HealthReady")
    .WithTags("Health");

app.MapGet("/health/sendgrid", async (IEmailHealthProbe probe, CancellationToken ct) =>
{
    var status = await probe.CheckAsync(ct);
    return status.IsHealthy
        ? Results.Ok(status)
        : Results.Problem(
            title: "SendGrid health check failed",
            detail: status.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
})
    .WithName("SendGridHealthCheck")
    .WithTags("Health");

if (app.Environment.IsDevelopment())
{
    app.MapGet("/health/sendgrid/key-trace", (IConfiguration cfg, IOptions<SendGridOptions> options) =>
    {
        var envApiKey = Environment.GetEnvironmentVariable("SENDGRID__APIKEY") ?? string.Empty;
        var configApiKey = cfg["SendGrid:ApiKey"] ?? string.Empty;
        var optionsApiKey = options.Value.ApiKey ?? string.Empty;

        var response = new
        {
            env = BuildKeySnapshot(envApiKey),
            config = BuildKeySnapshot(configApiKey),
            options = BuildKeySnapshot(optionsApiKey),
            sameFingerprint = !string.IsNullOrEmpty(envApiKey) &&
                              Fingerprint(envApiKey) == Fingerprint(configApiKey) &&
                              Fingerprint(configApiKey) == Fingerprint(optionsApiKey)
        };

        return Results.Ok(response);
    })
        .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
        .WithName("SendGridKeyTrace")
        .WithTags("Health");
}

app.MapGet("/health/liqpay/config", (ILiqPayPort port) =>
{
    var status = port.CheckConfig();
    return status.IsHealthy
        ? Results.Ok(status)
        : Results.Problem(
            title: "LiqPay config health check failed",
            detail: status.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
})
    .WithName("LiqPayConfigHealthCheck")
    .WithTags("Health");

app.MapGet("/health/liqpay", async (ILiqPayPort port, CancellationToken ct) =>
{
    var status = await port.CheckReadinessAsync(ct);
    return status.IsHealthy
        ? Results.Ok(status)
        : Results.Problem(
            title: "LiqPay readiness check failed",
            detail: status.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
})
    .WithName("LiqPayReadinessHealthCheck")
    .WithTags("Health");

app.Run();

/// <summary>
/// Docker Desktop інколи стартує контейнер API до того, як вбудований DNS (127.0.0.11) стабільно резолвить
/// імена сервісів — тоді перша міграція падає з "Name or service not known" і процес завершується (часто з кодом 139).
/// </summary>
static async Task InitializeDevelopmentDatabaseWithRetriesAsync(IServiceProvider services, ILogger logger)
{
    const int maxAttempts = 20;
    var delay = TimeSpan.FromSeconds(2);

    Exception? lastException = null;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await Marketplace.Infrastructure.DependencyInjection.InitializeDatabaseAsync(services);
            return;
        }
        catch (Exception ex) when (IsTransientDockerNetworkFailure(ex))
        {
            lastException = ex;
            if (attempt == maxAttempts)
                break;

            logger.LogWarning(ex,
                "Database migrate attempt {Attempt}/{Max} failed (transient network/DNS); retrying in {Delay}s.",
                attempt, maxAttempts, delay.TotalSeconds);
            await Task.Delay(delay);
        }
    }

    throw new InvalidOperationException(
        $"Database migration failed after {maxAttempts} attempts. " +
        "If this runs in Docker, ensure the API container is on the same Compose network as postgres " +
        "(service hostname 'postgres'). Try: docker compose down && docker compose up --build.",
        lastException);
}

static bool IsTransientDockerNetworkFailure(Exception ex)
{
    for (var e = ex; e is not null; e = e.InnerException)
    {
        if (e is SocketException se)
        {
            if (se.SocketErrorCode is SocketError.HostNotFound
                or SocketError.TryAgain
                or SocketError.NoRecovery
                or SocketError.ConnectionRefused)
                return true;
        }

        var msg = e.Message;
        if (msg.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase))
            return true;
        if (msg.Contains("nodename nor servname", StringComparison.OrdinalIgnoreCase))
            return true;
        if (msg.Contains("No such host is known", StringComparison.OrdinalIgnoreCase))
            return true;
        if (msg.Contains("Could not resolve host", StringComparison.OrdinalIgnoreCase))
            return true;
        if (msg.Contains("server misbehaving", StringComparison.OrdinalIgnoreCase))
            return true;
        if (msg.Contains("Connection refused", StringComparison.OrdinalIgnoreCase))
            return true;
    }

    return false;
}

static object BuildKeySnapshot(string value) => new
{
    present = !string.IsNullOrWhiteSpace(value),
    length = value.Length,
    startsWithSG = value.StartsWith("SG.", StringComparison.Ordinal),
    masked = Mask(value),
    fingerprint = Fingerprint(value)
};

static string Mask(string value)
{
    if (string.IsNullOrEmpty(value))
        return string.Empty;
    if (value.Length <= 8)
        return new string('*', value.Length);
    return $"{value[..4]}...{value[^4..]}";
}

static string Fingerprint(string value)
{
    if (string.IsNullOrEmpty(value))
        return string.Empty;
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
    return Convert.ToHexString(bytes)[..12];
}
