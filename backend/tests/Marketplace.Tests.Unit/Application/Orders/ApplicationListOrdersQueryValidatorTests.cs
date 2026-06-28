using Marketplace.Application.Orders.Queries.ListOrders;

namespace Marketplace.Tests;

[Trait("Suite", "Orders")]
public class ApplicationListOrdersQueryValidatorTests
{
    [Fact]
    public void Rejects_PageSize_Above_100()
    {
        var validator = new ListOrdersQueryValidator();
        var query = new ListOrdersQuery(
            OrderListScope.My,
            Guid.NewGuid(),
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            1,
            200);

        var result = validator.Validate(query);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Rejects_CompanyMemberId_For_My_Scope()
    {
        var validator = new ListOrdersQueryValidator();
        var query = new ListOrdersQuery(
            OrderListScope.My,
            Guid.NewGuid(),
            false,
            null,
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            1,
            20);

        var result = validator.Validate(query);
        Assert.False(result.IsValid);
    }
}
