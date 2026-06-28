namespace Marketplace.Application.Chats.Options;

public sealed class ChatsOptions
{
    public const string SectionName = "Chats";

    public bool Enabled { get; set; }
    public bool RealtimeEnabled { get; set; }
    public bool ModerationEnabled { get; set; }
    public int MessagesPerMinute { get; set; } = 20;
    public int DuplicateWindowSeconds { get; set; } = 30;
    public int MaxMessageLength { get; set; } = 4000;
    public bool RejectOnProhibitedContent { get; set; } = true;
    public string[] ProhibitedPatterns { get; set; } = [];
}
