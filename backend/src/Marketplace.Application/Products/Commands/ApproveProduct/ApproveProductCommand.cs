using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Commands.ApproveProduct;

public sealed record ApproveProductCommand(long ProductId, Guid ActorUserId) : IRequest<Result>;
