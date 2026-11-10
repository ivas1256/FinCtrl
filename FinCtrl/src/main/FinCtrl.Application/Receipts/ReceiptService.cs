using FinCtrl.Application.Payments;
using FinCtrl.Common.Application;
using FinCtrl.Domain.Receipts;

namespace FinCtrl.Application.Receipts;

public interface IReceiptService
{
    /// <summary>Чек (позиции и итоговая сумма) платежа; без позиций — пустой чек.</summary>
    /// <exception cref="NotFoundException">Платёж не существует или удалён.</exception>
    Task<Receipt> GetByPaymentId(int paymentId);
}

internal class ReceiptService(IPaymentRepository paymentRepository, IReceiptItemRepository receiptItemRepository) : IReceiptService
{
    public async Task<Receipt> GetByPaymentId(int paymentId)
    {
        _ = await paymentRepository.Get(paymentId)
            ?? throw new NotFoundException($"Операция {paymentId} не найдена");

        var items = await receiptItemRepository.GetByPaymentId(paymentId);

        return new Receipt(paymentId, items);
    }
}
