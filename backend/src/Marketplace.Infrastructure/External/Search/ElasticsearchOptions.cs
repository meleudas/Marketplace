namespace Marketplace.Infrastructure.External.Search;

public sealed class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    public bool Enabled { get; set; } = false;
    public string Url { get; set; } = "http://localhost:9200";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string ProductsIndex { get; set; } = "products-v1";
}
