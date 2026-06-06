using Marketplace.API.OpenApi;

namespace Marketplace.Tests.Platform;

public sealed class EndpointMarkdownParserTests
{
    [Fact]
    [Trait("Suite", "Platform")]
    public void ParseFile_extracts_summary_roles_and_idempotency_from_cart_checkout()
    {
        var markdown = """
            ## `POST /me/cart/checkout`

            - **Summary (1 рядок):** Checkout зі сплітом по компаніях.
            - **Призначення:** оформити checkout.
            - **Хто може викликати:**
              - JWT: обов'язково
              - Глобальні ролі: Buyer, Admin
              - Компанійні ролі: —
            - **Async / «магія»:**
              - Notifications: `AdminNewOrder`, `CompanyNewOrder`
            - **Де на фронті:**
              - API-модуль: `frontend/src/features/checkout/api/checkout.api.ts`
              - Статус: `planned`
            - **Idempotency:** обов'язковий заголовок `Idempotency-Key`
            """;

        var entries = EndpointMarkdownParser.ParseFile(markdown, "Cart.md");
        var entry = Assert.Single(entries);

        Assert.Equal("POST", entry.HttpMethod);
        Assert.Equal("/me/cart/checkout", entry.Path);
        Assert.Equal("Checkout зі сплітом по компаніях.", entry.Summary);
        Assert.Contains("Buyer", entry.GlobalRoles);
        Assert.Equal("planned", entry.FrontendStatus);
        Assert.True(entry.IdempotencyRequired);
        Assert.Contains("AdminNewOrder", entry.NotificationTemplates);
        Assert.Contains("AdminNewOrder", entry.BuildDescriptionMarkdown());
    }

    [Fact]
    [Trait("Suite", "Platform")]
    public void ParseFile_normalizes_route_constraints()
    {
        var markdown = """
            ## `GET /me/orders/{orderId}`

            - **Summary (1 рядок):** Деталі замовлення.
            - **Призначення:** buyer order detail.
            """;

        var entry = Assert.Single(EndpointMarkdownParser.ParseFile(markdown, "Orders.md"));

        Assert.Equal("/me/orders/{orderid}", entry.Path);
    }

    [Fact]
    [Trait("Suite", "Platform")]
    public void Registry_loads_cart_and_orders_docs_from_api_project()
    {
        var docsPath = ResolveApiDocsDirectory();
        Assert.True(Directory.Exists(docsPath), $"Docs directory not found: {docsPath}");

        var entries = Directory
            .EnumerateFiles(docsPath, "*.md")
            .Where(static path => !Path.GetFileName(path).Equals("README.md", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => EndpointMarkdownParser.ParseFile(File.ReadAllText(path), Path.GetFileName(path)))
            .ToArray();

        Assert.Contains(entries, entry => entry is { HttpMethod: "POST", Path: "/me/cart/checkout" });
        Assert.Contains(entries, entry => entry is { HttpMethod: "GET", Path: "/me/orders" });
    }

    [Fact]
    [Trait("Suite", "Platform")]
    public void ParseFile_supports_h3_endpoint_headers_from_admin_catalog()
    {
        var markdown = """
            ## Компанії

            ### `GET /admin/companies`

            - **Summary (1 рядок):** Повний список компаній для адмін-панелі.
            - **Призначення:** повний список компаній.
            - **Хто може викликати:**
              - JWT: обов'язково
              - Глобальні ролі: Admin
              - Компанійні ролі: —
            - **Async / «магія»:** —
            """;

        var entry = Assert.Single(EndpointMarkdownParser.ParseFile(markdown, "AdminCatalog.md"));

        Assert.Equal("GET", entry.HttpMethod);
        Assert.Equal("/admin/companies", entry.Path);
        Assert.Equal("Повний список компаній для адмін-панелі.", entry.Summary);
    }

    [Fact]
    [Trait("Suite", "Platform")]
    public void ParseFile_strips_query_string_from_documented_path()
    {
        var markdown = """
            ### `GET /products/{productId}/reviews?page=&size=`

            - **Summary (1 рядок):** Публічний список відгуків товару.
            - **Призначення:** список відгуків.
            """;

        var entry = Assert.Single(EndpointMarkdownParser.ParseFile(markdown, "Reviews.md"));

        Assert.Equal("/products/{productid}/reviews", entry.Path);
    }

    [Fact]
    [Trait("Suite", "Platform")]
    public void Registry_loads_admin_catalog_inventory_and_reviews_docs()
    {
        var docsPath = ResolveApiDocsDirectory();
        var entries = Directory
            .EnumerateFiles(docsPath, "*.md")
            .Where(static path => !Path.GetFileName(path).Equals("README.md", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => EndpointMarkdownParser.ParseFile(File.ReadAllText(path), Path.GetFileName(path)))
            .ToArray();

        Assert.Contains(entries, entry => entry is { HttpMethod: "GET", Path: "/admin/companies" });
        Assert.Contains(entries, entry => entry is { HttpMethod: "POST", Path: "/me/cart/coupons/apply" });
        Assert.Contains(entries, entry => entry is { HttpMethod: "GET", Path: "/companies/{companyid}/warehouses" });
        Assert.Contains(entries, entry => entry is { HttpMethod: "PUT", Path: "/reviews/products/{reviewid}/reply" });
    }

    private static string ResolveApiDocsDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "src", "Marketplace.API", "Docs", "Endpoints");
            if (Directory.Exists(candidate))
                return candidate;

            candidate = Path.Combine(current.FullName, "Docs", "Endpoints");
            if (Directory.Exists(candidate))
                return candidate;

            current = current.Parent;
        }

        throw new InvalidOperationException("Unable to locate Marketplace.API Docs/Endpoints directory.");
    }
}
