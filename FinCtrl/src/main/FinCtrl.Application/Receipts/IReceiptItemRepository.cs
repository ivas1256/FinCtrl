using FinCtrl.Domain.Receipts;

namespace FinCtrl.Application.Receipts;

public interface IReceiptItemRepository
{
    /// <summary>Позиции чека платежа в порядке <c>ReceiptItemId</c>; пустой список, если позиций нет.</summary>
    Task<List<ReceiptItem>> GetByPaymentId(int paymentId);
}
