using Marketplace.API.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;

namespace Marketplace.API.Extensions;

public static class OpenTelemetryLoggingExtensions
{
    public static ILoggingBuilder AddMarketplaceOpenTelemetryLogging(this ILoggingBuilder logging, IConfiguration configuration)
    {
        var options = configuration.GetSection(OpenTelemetryOptions.SectionName).Get<OpenTelemetryOptions>()
            ?? new OpenTelemetryOptions();

        if (!options.Enabled)
            return logging;

        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? options.OtlpEndpoint;
        if (string.IsNullOrWhiteSpace(otlpEndpoint))
            otlpEndpoint = "http://localhost:4317";

        logging.AddOpenTelemetry(openTelemetryLoggerOptions =>
        {
            openTelemetryLoggerOptions.IncludeFormattedMessage = true;
            openTelemetryLoggerOptions.IncludeScopes = true;
            openTelemetryLoggerOptions.AddOtlpExporter(exporterOptions =>
            {
                exporterOptions.Endpoint = new Uri(otlpEndpoint);
            });
        });

        return logging;
    }
}
