using Marketplace.Application.Common.Options;
using Marketplace.Infrastructure.External.Storage;

namespace Marketplace.Tests.Unit.Infrastructure.Storage;

public sealed class AwsS3ObjectStorageTests
{
    [Fact]
    [Trait("Suite", "Storage")]
    public void GetPublicUrl_Uses_PublicBaseUrl_Without_Bucket_Prefix()
    {
        var storage = new AwsS3ObjectStorage(
            client: null!,
            options: new StorageOptions
            {
                Provider = StorageProviders.AwsS3,
                Bucket = "marketplace-media",
                Region = "eu-central-1",
                PublicBaseUrl = "https://cdn.example.com/"
            });

        var url = storage.GetPublicUrl("products/a.webp");

        Assert.Equal("https://cdn.example.com/products/a.webp", url);
    }

    [Fact]
    [Trait("Suite", "Storage")]
    public void GetPublicUrl_Falls_Back_To_Virtual_Hosted_S3_Url()
    {
        var storage = new AwsS3ObjectStorage(
            client: null!,
            options: new StorageOptions
            {
                Provider = StorageProviders.AwsS3,
                Bucket = "marketplace-media",
                Region = "eu-central-1",
                PublicBaseUrl = ""
            });

        var url = storage.GetPublicUrl("products/a.webp");

        Assert.Equal("https://marketplace-media.s3.eu-central-1.amazonaws.com/products/a.webp", url);
    }
}
