namespace Marketplace.Domain.Catalog.Enums;

public enum ProductStatus : short
{
    Draft = 0,
    Active = 1,
    Archived = 2,
    /// <summary>Очікує схвалення модератором або адміністратором.</summary>
    PendingReview = 3
}
