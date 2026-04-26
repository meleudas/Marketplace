using FluentValidation;

namespace Marketplace.Application.Orders.Queries.ListOrders;

public sealed class ListOrdersQueryValidator : AbstractValidator<ListOrdersQuery>
{
    public ListOrdersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Sort).Must(x => x is null or "created_desc" or "created_asc" or "total_desc" or "total_asc")
            .WithMessage("Sort must be one of: created_desc, created_asc, total_desc, total_asc");
        RuleFor(x => x).Must(x => !x.CreatedFromUtc.HasValue || !x.CreatedToUtc.HasValue || x.CreatedFromUtc <= x.CreatedToUtc)
            .WithMessage("CreatedFromUtc must be less than or equal to CreatedToUtc");
    }
}
