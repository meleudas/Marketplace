using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.External.Storage;

public sealed class AwsS3ObjectStorage : IObjectStorage, IDisposable
{
    private readonly IAmazonS3 _client;
    private readonly StorageOptions _options;

    public AwsS3ObjectStorage(IOptions<StorageOptions> options)
    {
        _options = options.Value;
        _client = CreateClient(_options);
    }

    internal AwsS3ObjectStorage(IAmazonS3 client, StorageOptions options)
    {
        _client = client;
        _options = options;
    }

    public async Task EnsureBucketExistsAsync(CancellationToken ct = default)
    {
        try
        {
            await _client.HeadBucketAsync(new HeadBucketRequest { BucketName = _options.Bucket }, ct);
            return;
        }
        catch (AmazonS3Exception ex) when (IsMissingBucket(ex))
        {
            if (!_options.CreateBucketIfMissing)
                throw new InvalidOperationException(
                    $"S3 bucket '{_options.Bucket}' was not found and CreateBucketIfMissing=false.",
                    ex);
        }

        await _client.PutBucketAsync(new PutBucketRequest
        {
            BucketName = _options.Bucket,
            UseClientRegion = true
        }, ct);
    }

    public async Task UploadAsync(string objectKey, Stream content, string contentType, CancellationToken ct = default)
    {
        await EnsureBucketExistsAsync(ct);

        var request = new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        };
        await _client.PutObjectAsync(request, ct);
    }

    public async Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default)
    {
        await EnsureBucketExistsAsync(ct);

        using var response = await _client.GetObjectAsync(_options.Bucket, objectKey, ct);
        var memory = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memory, ct);
        memory.Position = 0;
        return memory;
    }

    public Task DeleteAsync(string objectKey, CancellationToken ct = default) =>
        _client.DeleteObjectAsync(_options.Bucket, objectKey, ct);

    public async Task<IReadOnlyList<string>> ListKeysAsync(string? prefix = null, CancellationToken ct = default)
    {
        await EnsureBucketExistsAsync(ct);

        var keys = new List<string>();
        string? continuationToken = null;
        do
        {
            var response = await _client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _options.Bucket,
                Prefix = prefix,
                ContinuationToken = continuationToken
            }, ct);

            foreach (var obj in response.S3Objects)
            {
                if (!string.IsNullOrWhiteSpace(obj.Key))
                    keys.Add(obj.Key);
            }

            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        } while (continuationToken is not null);

        return keys;
    }

    public string GetPublicUrl(string objectKey)
    {
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            return $"{_options.PublicBaseUrl.TrimEnd('/')}/{objectKey.TrimStart('/')}";

        var region = string.IsNullOrWhiteSpace(_options.Region) ? "eu-central-1" : _options.Region.Trim();
        return $"https://{_options.Bucket}.s3.{region}.amazonaws.com/{objectKey.TrimStart('/')}";
    }

    public Task<string> GetPresignedGetUrlAsync(string objectKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var ttlMinutes = Math.Max(1, _options.PresignedGetTtlMinutes);
        var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(ttlMinutes)
        });
        return Task.FromResult(url);
    }

    public void Dispose() => _client.Dispose();

    internal static IAmazonS3 CreateClient(StorageOptions options)
    {
        var region = string.IsNullOrWhiteSpace(options.Region) ? "eu-central-1" : options.Region.Trim();
        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(region)
        };

        // Explicit keys remain supported; empty keys use the default AWS credential chain (IAM role, env, profile).
        if (!string.IsNullOrWhiteSpace(options.AccessKey) && !string.IsNullOrWhiteSpace(options.SecretKey))
            return new AmazonS3Client(options.AccessKey, options.SecretKey, config);

        return new AmazonS3Client(config);
    }

    private static bool IsMissingBucket(AmazonS3Exception ex) =>
        ex.StatusCode == System.Net.HttpStatusCode.NotFound
        || string.Equals(ex.ErrorCode, "NoSuchBucket", StringComparison.OrdinalIgnoreCase)
        || string.Equals(ex.ErrorCode, "NotFound", StringComparison.OrdinalIgnoreCase);
}
