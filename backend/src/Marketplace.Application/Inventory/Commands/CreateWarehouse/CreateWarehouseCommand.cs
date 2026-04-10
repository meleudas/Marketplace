using Marketplace.Application.Inventory.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Commands.CreateWarehouse;

public sealed record CreateWarehouseCommand(
    Guid CompanyId,
    Guid ActorUserId,
    bool IsActorAdmin,
    string Name,
    string Code,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country,
    string TimeZone,
    int Priority) : IRequest<Result<WarehouseDto>>;
