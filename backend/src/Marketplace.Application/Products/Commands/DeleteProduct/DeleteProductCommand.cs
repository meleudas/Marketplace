using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(
    Guid CompanyId,
    long ProductId,
    Guid ActorUserId,
    bool IsActorAdmin) : IRequest<Result>;
