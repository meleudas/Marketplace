using Marketplace.Application.Common.Ports;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel;

namespace Marketplace.Infrastructure.External.Storage;

public sealed class MinioObjectStorage : IObjectStorage
{
    private readonly IMinioClient _client;
    private readonly StorageOptions _options;

    public MinioObjectStorage(IOptions<StorageOptions> options)
    {
        _options = options.Value;
        _client = new MinioClient()
            .WithEndpoint(_options.Endpoint)
            .WithCredentials(_options.AccessKey, _options.SecretKey)
            .WithSSL(_options.UseSsl)
            .Build();
    }

    public async Task EnsureBucketExistsAsync(CancellationToken ct = default)
    {
        var exists = await _client.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_options.Bucket),
            ct);

        if (!exists)
        {
            await _client.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_options.Bucket),
                ct);
        }
    }

    public async Task UploadAsync(string objectKey, Stream content, string contentType, CancellationToken ct = default)
    {
        await EnsureBucketExistsAsync(ct);
        await _client.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(objectKey)
                .WithStreamData(content)
                .WithObjectSize(content.Length)
                .WithContentType(contentType),
            ct);
    }

    public async Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default)
    {
        await EnsureBucketExistsAsync(ct);
        var memory = new MemoryStream();
        await _client.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(objectKey)
                .WithCallbackStream(stream => stream.CopyTo(memory)),
            ct);
        memory.Position = 0;
        return memory;
    }

    public async Task DeleteAsync(string objectKey, CancellationToken ct = default)
    {
        await _client.RemoveObjectAsync(
            new RemoveObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(objectKey),
            ct);
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string? prefix = null, CancellationToken ct = default)
    {
        await EnsureBucketExistsAsync(ct);
        var items = new List<string>();
        var args = new ListObjectsArgs()
            .WithBucket(_options.Bucket)
            .WithRecursive(true);
        if (!string.IsNullOrWhiteSpace(prefix))
            args = args.WithPrefix(prefix);

        var observable = _client.ListObjectsEnumAsync(args, ct);
        await foreach (Item item in observable.WithCancellation(ct))
        {
            if (!string.IsNullOrWhiteSpace(item.Key))
                items.Add(item.Key);
        }

        return items;
    }

    public string GetPublicUrl(string objectKey)
    {
        var baseUrl = _options.PublicBaseUrl.TrimEnd('/');
        return $"{baseUrl}/{_options.Bucket}/{objectKey}";
    }

    public async Task<string> GetPresignedGetUrlAsync(string objectKey, CancellationToken ct = default)
    {
        await EnsureBucketExistsAsync(ct);
        var ttlSeconds = Math.Max(1, _options.PresignedGetTtlMinutes * 60);
        return await _client.PresignedGetObjectAsync(
            new PresignedGetObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(objectKey)
                .WithExpiry(ttlSeconds));
    }
}
