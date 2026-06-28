using Marketplace.API.Options;
using Marketplace.Application.Common.Observability;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.EntityFrameworkCore;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Instrumentation.Runtime;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Marketplace.API.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddMarketplaceOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        var options = configuration.GetSection(OpenTelemetryOptions.SectionName).Get<OpenTelemetryOptions>()
            ?? new OpenTelemetryOptions();

        if (!options.Enabled)
            return services;

        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? options.OtlpEndpoint;
        if (string.IsNullOrWhiteSpace(otlpEndpoint))
            otlpEndpoint = "http://localhost:4317";

        var serviceName = configuration["OTEL_SERVICE_NAME"] ?? options.ServiceName;
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? hostEnvironment.EnvironmentName;
        var serviceVersion = typeof(OpenTelemetryExtensions).Assembly.GetName().Version?.ToString() ?? "unknown";
        void ConfigureOtlpExporter(OtlpExporterOptions exporterOptions)
        {
            exporterOptions.Endpoint = new Uri(otlpEndpoint);
        }

        var otelBuilder = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddAttributes(new Dictionary<string, object>
            {
                ["service.name"] = serviceName,
                ["service.version"] = serviceVersion,
                ["deployment.environment"] = environment
            }))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(MarketplaceMetrics.MeterName)
                    .AddOtlpExporter(ConfigureOtlpExporter);

                if (options.EnableLegacyPrometheusEndpoint)
                    metrics.AddPrometheusExporter();
            })
            .WithTracing(tracing =>
            {
                tracing
                    .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(
                        Math.Clamp(options.TraceSamplingRatio, 0.0, 1.0))))
                    .AddSource(MarketplaceTelemetry.ActivitySourceName)
                    .AddAspNetCoreInstrumentation(aspNetOptions =>
                    {
                        aspNetOptions.Filter = httpContext =>
                        {
                            var path = httpContext.Request.Path.Value ?? string.Empty;
                            return !IsExcludedTelemetryPath(path);
                        };
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddOtlpExporter(ConfigureOtlpExporter);
            });

        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
            otelBuilder.WithTracing(tracing => tracing.AddRedisInstrumentation());

        return services;
    }

    private static bool IsExcludedTelemetryPath(string path) =>
        path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/hangfire", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase);
}
