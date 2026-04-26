using Marketplace.Application.Orders.Queries.ListOrders;

namespace Marketplace.Tests;

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
            1,
            200);

        var result = validator.Validate(query);
        Assert.False(result.IsValid);
    }
}
