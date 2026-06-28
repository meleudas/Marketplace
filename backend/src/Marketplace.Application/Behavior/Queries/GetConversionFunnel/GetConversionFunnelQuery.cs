using Marketplace.Application.Behavior.DTOs;
using Marketplace.Domain.Behavior.Enums;
using Marketplace.Domain.Behavior.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Behavior.Queries.GetConversionFunnel;

public sealed record GetConversionFunnelQuery(DateOnly FromDate, DateOnly ToDate) : IRequest<Result<ConversionFunnelDto>>;

public sealed class GetConversionFunnelQueryHandler : IRequestHandler<GetConversionFunnelQuery, Result<ConversionFunnelDto>>
{
    private readonly IBehaviorEventRepository _repository;

    public GetConversionFunnelQueryHandler(IBehaviorEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ConversionFunnelDto>> Handle(GetConversionFunnelQuery request, CancellationToken ct)
    {
        var from = request.FromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = request.ToDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var views = await _repository.CountByTypeAsync(BehaviorEventType.ProductView, from, to, ct);
        var clicks = await _repository.CountByTypeAsync(BehaviorEventType.CatalogClick, from, to, ct);
        var adds = await _repository.CountByTypeAsync(BehaviorEventType.AddToCart, from, to, ct);
        var ctr = views == 0 ? 0 : decimal.Round((decimal)clicks / views, 4);
        var atc = clicks == 0 ? 0 : decimal.Round((decimal)adds / clicks, 4);
        return Result<ConversionFunnelDto>.Success(new ConversionFunnelDto(views, clicks, adds, ctr, atc));
    }
}
