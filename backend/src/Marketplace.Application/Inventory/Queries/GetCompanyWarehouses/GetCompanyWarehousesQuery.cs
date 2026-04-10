using Marketplace.Application.Inventory.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Queries.GetCompanyWarehouses;

public sealed record GetCompanyWarehousesQuery(
    Guid CompanyId,
    Guid ActorUserId,
    bool IsActorAdmin) : IRequest<Result<IReadOnlyList<WarehouseDto>>>;
