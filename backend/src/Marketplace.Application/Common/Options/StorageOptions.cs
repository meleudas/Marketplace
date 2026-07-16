namespace Marketplace.Application.Common.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Storage backend: <c>Minio</c> (default, access/secret keys) or <c>AwsS3</c> (IAM role / default credential chain).
    /// </summary>
    public string Provider { get; set; } = StorageProviders.Minio;

    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = "localhost:9000";

    /// <summary>
    /// Required for Provider=Minio. For Provider=AwsS3 leave empty to use IAM role / default credential chain.
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Required for Provider=Minio. For Provider=AwsS3 leave empty to use IAM role / default credential chain.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    public string Bucket { get; set; } = "marketplace-media";
    public string Region { get; set; } = "eu-central-1";
    public bool UseSsl { get; set; }

    /// <summary>
    /// When true, EnsureBucketExists creates the bucket if missing.
    /// Prefer false for AWS (bucket provisioned by infra); true for local MinIO.
    /// </summary>
    public bool CreateBucketIfMissing { get; set; } = true;

    public string PublicBaseUrl { get; set; } = "http://localhost:9000";
    public int PresignedGetTtlMinutes { get; set; } = 60;
    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;
    public double DerivativeMaxToOriginalRatio { get; set; } = 1.0;
    public int[] DerivativeWebpQualities { get; set; } = [90, 82, 74, 66, 58, 50];
    public string MediaPrefix { get; set; } = "products/";

    public bool IsAwsS3() =>
        string.Equals(Provider, StorageProviders.AwsS3, StringComparison.OrdinalIgnoreCase);

    public bool IsMinio() =>
        string.IsNullOrWhiteSpace(Provider)
        || string.Equals(Provider, StorageProviders.Minio, StringComparison.OrdinalIgnoreCase);
}

public static class StorageProviders
{
    public const string Minio = "Minio";
    public const string AwsS3 = "AwsS3";
}
