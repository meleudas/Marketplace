namespace Marketplace.Application.Auth.DTOs;

/// <summary>Стан 2FA з ASP.NET Identity для залогіненого користувача.</summary>
public record TwoFactorStatusDto(
    bool TwoFactorEnabled,
    bool TelegramTwoFactorEnabled,
    bool TelegramLinked);
