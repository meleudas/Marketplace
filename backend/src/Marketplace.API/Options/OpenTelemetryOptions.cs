namespace Marketplace.API.Options;

public sealed class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public bool Enabled { get; set; } = true;

    public string ServiceName { get; set; } = "marketplace-api";

    public string OtlpEndpoint { get; set; } = "http://localhost:4317";

    public string OtlpProtocol { get; set; } = "grpc";

    public bool EnableLegacyPrometheusEndpoint { get; set; }

    public double TraceSamplingRatio { get; set; } = 1.0;
}
