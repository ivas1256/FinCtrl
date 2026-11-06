using FinCtrl.Domain;

namespace FinCtrl.Application.Transfers;

/// <summary>Хранилище переводов между своими счетами (таблица <c>Transfers</c>).</summary>
public interface ITransferRepository
{
    /// <summary>Перевод, в который входит платёж (как списание или как поступление), или <c>null</c>.</summary>
    Task<Transfer?> GetByPaymentId(int paymentId);

    /// <summary>Создаёт перевод.</summary>
    /// <returns>Id созданного перевода.</returns>
    Task<int> Create(Transfer transfer);

    /// <summary>Сохраняет стороны перевода.</summary>
    Task Update(Transfer transfer);

    /// <summary>Удаляет перевод (сами платежи не трогает).</summary>
    Task Delete(int transferId);
}
