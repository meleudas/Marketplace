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

namespace Marketplace.Application.Support.Commands.AddSupportMessage;

public sealed record AddSupportMessageCommand(
    string ActorUserId,
    long TicketId,
    string Message,
    bool IsInternal,
    bool IsStaff) : IRequest<Result<SupportTicketMessageDto>>;

public sealed class AddSupportMessageCommandValidator : AbstractValidator<AddSupportMessageCommand>
{
    public AddSupportMessageCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.TicketId).GreaterThan(0);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(8000);
    }
}

public sealed class AddSupportMessageCommandHandler : IRequestHandler<AddSupportMessageCommand, Result<SupportTicketMessageDto>>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ISupportTicketMessageRepository _messageRepository;
    private readonly ISupportTicketEventRepository _eventRepository;
    private readonly SupportTicketAccessPolicy _accessPolicy;
    private readonly SupportHelpdeskOutboxPublisher _outboxPublisher;
    private readonly SupportOptions _options;

    public AddSupportMessageCommandHandler(
        ISupportTicketRepository ticketRepository,
        ISupportTicketMessageRepository messageRepository,
        ISupportTicketEventRepository eventRepository,
        SupportTicketAccessPolicy accessPolicy,
        SupportHelpdeskOutboxPublisher outboxPublisher,
        IOptions<SupportOptions> options)
    {
        _ticketRepository = ticketRepository;
        _messageRepository = messageRepository;
        _eventRepository = eventRepository;
        _accessPolicy = accessPolicy;
        _outboxPublisher = outboxPublisher;
        _options = options.Value;
    }

    public async Task<Result<SupportTicketMessageDto>> Handle(AddSupportMessageCommand request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return Result<SupportTicketMessageDto>.Failure("conflict: support is disabled");

        var ticket = await _accessPolicy.GetAccessibleTicketAsync(request.TicketId, request.ActorUserId, request.IsStaff, ct);
        if (ticket is null)
            return Result<SupportTicketMessageDto>.Failure("forbidden: access denied");

        if (!await _accessPolicy.CanWriteMessageAsync(ticket, request.ActorUserId, request.IsStaff, request.IsInternal, ct))
            return Result<SupportTicketMessageDto>.Failure("forbidden: access denied");

        if (ticket.Status == SupportTicketStatus.Closed)
            return Result<SupportTicketMessageDto>.Failure("conflict: ticket is closed");

        var now = DateTime.UtcNow;
        var message = SupportTicketMessage.Send(
            ticket.Id,
            request.ActorUserId,
            request.Message,
            JsonBlob.Empty,
            request.IsInternal,
            now);

        var saved = await _messageRepository.AddAsync(message, ct);
        if (!request.IsInternal)
            ticket.RecordCustomerMessage(saved.Message, now);
        await _ticketRepository.UpdateAsync(ticket, ct);

        await _eventRepository.AppendAsync(
            SupportTicketEvent.Create(
                ticket.Id,
                SupportTicketEventType.MessageAdded,
                request.ActorUserId,
                request.IsInternal ? "internal message" : "message",
                new JsonBlob(System.Text.Json.JsonSerializer.Serialize(new { messageId = saved.Id.Value })),
                now),
            ct);

        await _outboxPublisher.PublishMessageAddedAsync(ticket.Id.Value, saved.Id.Value, ct);
        return Result<SupportTicketMessageDto>.Success(saved.ToDto());
    }
}
