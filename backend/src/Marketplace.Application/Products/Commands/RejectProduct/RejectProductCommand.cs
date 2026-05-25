using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Commands.RejectProduct;

public sealed record RejectProductCommand(long ProductId, Guid ActorUserId, string? Reason) : IRequest<Result>;
