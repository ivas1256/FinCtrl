namespace FinCtrl.Domain.Receipts;

/// <summary>Чек платежа: позиции и их итоговая сумма. Read-модель.</summary>
public class Receipt
{
    public Receipt(int paymentId, IReadOnlyList<ReceiptItem> items)
    {
        PaymentId = paymentId;
        Items = items;
    }

    public int PaymentId { get; }
    public IReadOnlyList<ReceiptItem> Items { get; }
    /// <summary>Сумма <see cref="ReceiptItem.TotalPrice"/> позиций; 0 для пустого чека.</summary>
    public decimal TotalSum => Items.Sum(i => i.TotalPrice);
}
