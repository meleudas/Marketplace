using FluentValidation;
using Marketplace.Application.Common.Observability;
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

namespace Marketplace.Application.Support.Commands.EscalateSupportTicket;

public sealed record EscalateSupportTicketCommand(
    string ActorUserId,
    long TicketId,
    string Reason) : IRequest<Result<SupportTicketDto>>;

public sealed class EscalateSupportTicketCommandValidator : AbstractValidator<EscalateSupportTicketCommand>
{
    public EscalateSupportTicketCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.TicketId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
    }
}

public sealed class EscalateSupportTicketCommandHandler : IRequestHandler<EscalateSupportTicketCommand, Result<SupportTicketDto>>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ISupportTicketEventRepository _eventRepository;
    private readonly SupportEscalationPolicy _escalationPolicy;
    private readonly SupportHelpdeskOutboxPublisher _outboxPublisher;
    private readonly SupportOptions _options;

    public EscalateSupportTicketCommandHandler(
        ISupportTicketRepository ticketRepository,
        ISupportTicketEventRepository eventRepository,
        SupportEscalationPolicy escalationPolicy,
        SupportHelpdeskOutboxPublisher outboxPublisher,
        IOptions<SupportOptions> options)
    {
        _ticketRepository = ticketRepository;
        _eventRepository = eventRepository;
        _escalationPolicy = escalationPolicy;
        _outboxPublisher = outboxPublisher;
        _options = options.Value;
    }

    public async Task<Result<SupportTicketDto>> Handle(EscalateSupportTicketCommand request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return Result<SupportTicketDto>.Failure("conflict: support is disabled");

        var ticket = await _ticketRepository.GetByIdAsync(SupportTicketId.From(request.TicketId), ct);
        if (ticket is null)
            return Result<SupportTicketDto>.Failure("not found: ticket not found");

        var now = DateTime.UtcNow;
        if (ticket.IsSlaBreached(now) || _escalationPolicy.ShouldEscalate(ticket.Priority, ticket.CreatedAt, now))
            MarketplaceMetrics.SupportSlaBreachTotal.Add(1);

        var escalate = ticket.Escalate(request.ActorUserId, request.Reason, now);
        if (escalate.IsFailure)
            return Result<SupportTicketDto>.Failure(escalate.Error ?? "conflict");

        await _ticketRepository.UpdateAsync(ticket, ct);
        await _eventRepository.AppendAsync(
            SupportTicketEvent.Create(
                ticket.Id,
                SupportTicketEventType.Escalated,
                request.ActorUserId,
                request.Reason,
                new JsonBlob("{}"),
                now),
            ct);

        await _outboxPublisher.PublishStatusChangedAsync(ticket.Id.Value, (short)ticket.Status, ct);
        return Result<SupportTicketDto>.Success(ticket.ToDto(now));
    }
}
