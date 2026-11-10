using FinCtrl.Application.Receipts;
using FinCtrl.Common.Infrastructure;
using FinCtrl.Domain.Receipts;

namespace FinCtrl.Infrastructure.Receipts;

internal class ReceiptItemRepository(IDbConnectionFactory dbConnectionFactory) : RepositoryBase(dbConnectionFactory), IReceiptItemRepository
{
    private const string _select = @"SELECT
                  ri.ReceiptItemId
                 ,ri.PaymentId
                 ,ri.ItemName
                 ,ri.Price
                 ,ri.Quantity
                 ,ri.UnitId
                 ,u.UnitName
                 ,ri.TotalPrice
                 ,ri.CreatedAt
                 ,ri.LastUpdatedAt
              FROM ReceiptItems ri
              JOIN Units u ON u.UnitId = ri.UnitId";

    public async Task<List<ReceiptItem>> GetByPaymentId(int paymentId)
    {
        return (await Query<ReceiptItem>(_select + " WHERE ri.PaymentId = @paymentId ORDER BY ri.ReceiptItemId", new { paymentId })).ToList();
    }
}
