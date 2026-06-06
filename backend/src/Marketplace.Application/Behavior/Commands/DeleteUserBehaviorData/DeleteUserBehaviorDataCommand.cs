using Marketplace.Domain.Behavior.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Behavior.Commands.DeleteUserBehaviorData;

public sealed record DeleteUserBehaviorDataCommand(Guid UserId) : IRequest<Result>;

public sealed class DeleteUserBehaviorDataCommandHandler : IRequestHandler<DeleteUserBehaviorDataCommand, Result>
{
    private readonly IBehaviorEventRepository _repository;

    public DeleteUserBehaviorDataCommandHandler(IBehaviorEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(DeleteUserBehaviorDataCommand request, CancellationToken ct)
    {
        await _repository.SoftDeleteByUserIdAsync(request.UserId, DateTime.UtcNow, ct);
        return Result.Success();
    }
}
