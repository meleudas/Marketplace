using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Tests;

public class DomainProductTests
{
    [Fact]
    public void Product_Lifecycle_Works_Without_Admin_Approval()
    {
        var product = Product.Create(
            ProductId.From(1),
            CompanyId.From(Guid.NewGuid()),
            "Name",
            "name",
            "Description",
            new Money(100),
            null,
            0,
            1,
            CategoryId.From(10),
            false);

        Assert.Equal(ProductStatus.Draft, product.Status);
        product.Activate();
        Assert.Equal(ProductStatus.Active, product.Status);
        product.Archive();
        Assert.Equal(ProductStatus.Archived, product.Status);
    }
}
