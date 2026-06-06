namespace Marketplace.Application.Auth.DTOs;

public sealed record TelegramUpdateDto(TelegramMessageDto? Message);
public sealed record TelegramMessageDto(TelegramChatDto? Chat, string? Text);
public sealed record TelegramChatDto(long Id);
