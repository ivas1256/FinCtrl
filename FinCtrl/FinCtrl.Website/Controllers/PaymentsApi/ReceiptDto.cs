using FinCtrl.Domain.Receipts;

namespace FinCtrl.Website.Controllers.PaymentsApi;

public class ReceiptDto
{
    public int PaymentId { get; set; }
    public decimal TotalSum { get; set; }
    public List<ReceiptItemDto> Items { get; set; } = [];

    public static ReceiptDto FromModel(Receipt receipt) => new()
    {
        PaymentId = receipt.PaymentId,
        TotalSum = receipt.TotalSum,
        Items = receipt.Items.Select(ReceiptItemDto.FromModel).ToList()
    };
}

public class ReceiptItemDto
{
    public int ReceiptItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }

    public static ReceiptItemDto FromModel(ReceiptItem item) => new()
    {
        ReceiptItemId = item.ReceiptItemId,
        ItemName = item.ItemName,
        Price = item.Price,
        Quantity = item.Quantity,
        UnitId = item.UnitId,
        UnitName = item.UnitName,
        TotalPrice = item.TotalPrice
    };
}
