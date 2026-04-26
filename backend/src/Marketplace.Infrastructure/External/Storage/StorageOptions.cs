namespace Marketplace.Infrastructure.External.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public bool Enabled { get; set; } = false;
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public string Bucket { get; set; } = "marketplace-media";
    public bool UseSsl { get; set; } = false;
    public string PublicBaseUrl { get; set; } = "http://localhost:9000";
    public int PresignedGetTtlMinutes { get; set; } = 60;
    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;
    public double DerivativeMaxToOriginalRatio { get; set; } = 1.0;
    public int[] DerivativeWebpQualities { get; set; } = [90, 82, 74, 66, 58, 50];
    public string MediaPrefix { get; set; } = "products/";
}
