namespace Marketplace.Domain.Orders.Enums;

/// <summary>Спосіб оплати на замовленні (поле orders.payment_method у схемі).</summary>
public enum CheckoutPaymentMethod : short
{
    Card = 0,
    PayPal = 1,
    BankTransfer = 2,
    Cash = 3
}
