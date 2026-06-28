using FluentValidation;
using Marketplace.Application.Chats;
using Marketplace.Application.Chats.DTOs;
using Marketplace.Application.Chats.Options;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Chats.Entities;
using Marketplace.Domain.Chats.Enums;
using Marketplace.Domain.Chats.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Chats.Commands.CreateChat;

public sealed record CreateChatCommand(
    Guid ActorUserId,
    short Type,
    long? ProductId,
    long? OrderId) : IRequest<Result<ChatDto>>;

public sealed class CreateChatCommandValidator : AbstractValidator<CreateChatCommand>
{
    public CreateChatCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Type).InclusiveBetween((short)0, (short)2);
    }
}

public sealed class CreateChatCommandHandler : IRequestHandler<CreateChatCommand, Result<ChatDto>>
{
    private readonly IChatRepository _chatRepository;
    private readonly IChatParticipantRepository _participantRepository;
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICompanyMemberRepository _companyMemberRepository;
    private readonly ChatsOptions _options;

    public CreateChatCommandHandler(
        IChatRepository chatRepository,
        IChatParticipantRepository participantRepository,
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        ICompanyMemberRepository companyMemberRepository,
        IOptions<ChatsOptions> options)
    {
        _chatRepository = chatRepository;
        _participantRepository = participantRepository;
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _companyMemberRepository = companyMemberRepository;
        _options = options.Value;
    }

    public async Task<Result<ChatDto>> Handle(CreateChatCommand request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return Result<ChatDto>.Failure("conflict: chats are disabled");

        var now = DateTime.UtcNow;
        var chatType = (ChatType)request.Type;

        return chatType switch
        {
            ChatType.Direct => await CreateDirectAsync(request, now, ct),
            ChatType.OrderRelated => await CreateOrderRelatedAsync(request, now, ct),
            ChatType.Support => await CreateSupportAsync(request, now, ct),
            _ => Result<ChatDto>.Failure("Invalid chat type")
        };
    }

    private async Task<Result<ChatDto>> CreateDirectAsync(CreateChatCommand request, DateTime now, CancellationToken ct)
    {
        if (!request.ProductId.HasValue)
            return Result<ChatDto>.Failure("ProductId is required for direct chat");

        var productId = ProductId.From(request.ProductId.Value);
        var product = await _productRepository.GetByIdAsync(productId, ct);
        if (product is null)
            return Result<ChatDto>.Failure("Product not found");

        var existing = await _chatRepository.FindActiveDirectAsync(productId, request.ActorUserId, ct);
        if (existing is not null)
            return Result<ChatDto>.Success(existing.ToDto());

        var seller = await ResolveSellerUserAsync(product.CompanyId, ct);
        if (seller is null)
            return Result<ChatDto>.Failure("Seller not found for product");

        var chat = Chat.CreateDirect(request.ActorUserId, productId, now);
        var saved = await _chatRepository.AddAsync(chat, ct);
        await AddParticipantsAsync(saved.Id, request.ActorUserId, seller.Value, product.CompanyId, now, ct);
        return Result<ChatDto>.Success(saved.ToDto());
    }

    private async Task<Result<ChatDto>> CreateOrderRelatedAsync(CreateChatCommand request, DateTime now, CancellationToken ct)
    {
        if (!request.OrderId.HasValue)
            return Result<ChatDto>.Failure("OrderId is required for order chat");

        var orderId = OrderId.From(request.OrderId.Value);
        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order is null)
            return Result<ChatDto>.Failure("Order not found");
        if (order.CustomerId != request.ActorUserId)
            return Result<ChatDto>.Failure("forbidden: order does not belong to actor");

        var existing = await _chatRepository.FindActiveOrderRelatedAsync(orderId, request.ActorUserId, ct);
        if (existing is not null)
            return Result<ChatDto>.Success(existing.ToDto());

        var seller = await ResolveSellerUserAsync(order.CompanyId, ct);
        if (seller is null)
            return Result<ChatDto>.Failure("Seller not found for order");

        var chat = Chat.CreateOrderRelated(request.ActorUserId, orderId, now);
        var saved = await _chatRepository.AddAsync(chat, ct);
        await AddParticipantsAsync(saved.Id, request.ActorUserId, seller.Value, order.CompanyId, now, ct);
        return Result<ChatDto>.Success(saved.ToDto());
    }

    private async Task<Result<ChatDto>> CreateSupportAsync(CreateChatCommand request, DateTime now, CancellationToken ct)
    {
        var existing = await _chatRepository.FindActiveSupportAsync(request.ActorUserId, ct);
        if (existing is not null)
            return Result<ChatDto>.Success(existing.ToDto());

        var chat = Chat.CreateSupport(request.ActorUserId, now);
        var saved = await _chatRepository.AddAsync(chat, ct);
        await _participantRepository.AddAsync(
            ChatParticipant.Join(saved.Id, request.ActorUserId, ChatParticipantRole.Buyer, null, now),
            ct);
        return Result<ChatDto>.Success(saved.ToDto());
    }

    private async Task<Guid?> ResolveSellerUserAsync(CompanyId companyId, CancellationToken ct)
    {
        var members = await _companyMemberRepository.ListByCompanyAsync(companyId, ct);
        var active = members.Where(x => !x.IsDeleted).ToList();
        var owner = active.FirstOrDefault(x => x.IsOwner);
        return owner?.UserId ?? active.FirstOrDefault()?.UserId;
    }

    private async Task AddParticipantsAsync(
        ChatId chatId,
        Guid buyerId,
        Guid sellerId,
        CompanyId companyId,
        DateTime now,
        CancellationToken ct)
    {
        var participants = new List<ChatParticipant>
        {
            ChatParticipant.Join(chatId, buyerId, ChatParticipantRole.Buyer, null, now),
            ChatParticipant.Join(chatId, sellerId, ChatParticipantRole.Seller, companyId, now)
        };
        await _participantRepository.AddRangeAsync(participants, ct);
    }
}
