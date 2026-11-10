namespace FinCtrl.Domain.Receipts;

/// <summary>Позиция чека платежа.</summary>
public class ReceiptItem
{
    public int ReceiptItemId { get; set; }
    public int PaymentId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public int UnitId { get; set; }
    /// <summary>Название единицы измерения (из справочника Units).</summary>
    public string UnitName { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}
