namespace Marketplace.Domain.Returns.Enums;

public enum ReturnReasonCode : short
{
    Defective = 1,
    WrongItem = 2,
    NotAsDescribed = 3,
    ChangedMind = 4,
    DamagedInShipping = 5,
    Other = 99
}
