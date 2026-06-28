using FluentValidation;
using Marketplace.Application.Support;
using Marketplace.Application.Support.DTOs;
using Marketplace.Application.Support.Options;
using Marketplace.Application.Support.Policies;
using Marketplace.Application.Support.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Support.Entities;
using Marketplace.Domain.Support.Enums;
using Marketplace.Domain.Support.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Support.Commands.AssignSupportTicket;

public sealed record AssignSupportTicketCommand(
    string ActorUserId,
    long TicketId,
    string AssigneeUserId,
    string Reason) : IRequest<Result<SupportTicketDto>>;

public sealed class AssignSupportTicketCommandValidator : AbstractValidator<AssignSupportTicketCommand>
{
    public AssignSupportTicketCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.TicketId).GreaterThan(0);
        RuleFor(x => x.AssigneeUserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
    }
}

public sealed class AssignSupportTicketCommandHandler : IRequestHandler<AssignSupportTicketCommand, Result<SupportTicketDto>>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ISupportTicketAssignmentRepository _assignmentRepository;
    private readonly ISupportTicketEventRepository _eventRepository;
    private readonly SupportTicketAccessPolicy _accessPolicy;
    private readonly SupportHelpdeskOutboxPublisher _outboxPublisher;
    private readonly SupportOptions _options;

    public AssignSupportTicketCommandHandler(
        ISupportTicketRepository ticketRepository,
        ISupportTicketAssignmentRepository assignmentRepository,
        ISupportTicketEventRepository eventRepository,
        SupportTicketAccessPolicy accessPolicy,
        SupportHelpdeskOutboxPublisher outboxPublisher,
        IOptions<SupportOptions> options)
    {
        _ticketRepository = ticketRepository;
        _assignmentRepository = assignmentRepository;
        _eventRepository = eventRepository;
        _accessPolicy = accessPolicy;
        _outboxPublisher = outboxPublisher;
        _options = options.Value;
    }

    public async Task<Result<SupportTicketDto>> Handle(AssignSupportTicketCommand request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return Result<SupportTicketDto>.Failure("conflict: support is disabled");

        if (!await _accessPolicy.CanManageAsync(isStaff: true))
            return Result<SupportTicketDto>.Failure("forbidden: access denied");

        var ticket = await _ticketRepository.GetByIdAsync(SupportTicketId.From(request.TicketId), ct);
        if (ticket is null)
            return Result<SupportTicketDto>.Failure("not found: ticket not found");

        var now = DateTime.UtcNow;
        var assignResult = ticket.Assign(request.AssigneeUserId, request.ActorUserId, request.Reason, now);
        if (assignResult.IsFailure)
            return Result<SupportTicketDto>.Failure(assignResult.Error ?? "conflict");

        await _ticketRepository.UpdateAsync(ticket, ct);
        await _assignmentRepository.AppendAsync(
            SupportTicketAssignment.Create(ticket.Id, request.AssigneeUserId, request.ActorUserId, request.Reason, now),
            ct);
        await _eventRepository.AppendAsync(
            SupportTicketEvent.Create(
                ticket.Id,
                SupportTicketEventType.Assigned,
                request.ActorUserId,
                request.Reason,
                new JsonBlob(System.Text.Json.JsonSerializer.Serialize(new { assigneeUserId = request.AssigneeUserId })),
                now),
            ct);

        await _outboxPublisher.PublishStatusChangedAsync(ticket.Id.Value, (short)ticket.Status, ct);
        return Result<SupportTicketDto>.Success(ticket.ToDto(now));
    }
}
