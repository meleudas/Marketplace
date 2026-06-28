namespace Marketplace.Domain.Chats.Enums;

public enum MessageStatus : short
{
    Sent = 0,
    Delivered = 1,
    Read = 2,
    DeletedForPolicy = 3
}
