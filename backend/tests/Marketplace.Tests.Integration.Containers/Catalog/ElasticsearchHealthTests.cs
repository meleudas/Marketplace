using System.Net;
using Marketplace.Tests.Fixtures;
using Xunit;

namespace Marketplace.Tests.Catalog;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "CatalogCategories")]
[Trait("Layer", "IntegrationContainers")]
public sealed class ElasticsearchHealthTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public ElasticsearchHealthTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Elasticsearch_Container_Is_Reachable()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var response = await http.GetAsync($"{_fixture.ElasticsearchUrl.TrimEnd('/')}/_cluster/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
