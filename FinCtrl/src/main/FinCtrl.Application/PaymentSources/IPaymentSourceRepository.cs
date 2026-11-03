using FinCtrl.Domain;

namespace FinCtrl.Application.PaymentSources;

/// <summary>Хранилище источников платежа (контрагентов из банковского файла).</summary>
public interface IPaymentSourceRepository
{
    /// <summary>Все источники вместе с их категориями.</summary>
    Task<List<PaymentSource>> GetAll();

    /// <summary>Источник по id или <c>null</c>, если не найден.</summary>
    Task<PaymentSource?> GetById(int paymentSourceId);

    /// <summary>Источник по имени или <c>null</c>, если не найден.</summary>
    Task<PaymentSource?> GetByName(string paymentSourceName);

    /// <summary>Создаёт источник.</summary>
    /// <returns>Id созданного источника.</returns>
    Task<int> Create(PaymentSource paymentSource);

    /// <summary>Сохраняет изменения источников.</summary>
    Task Update(params PaymentSource[] paymentSources);

    /// <summary>Сохраняет только классификацию и признак ИИ-классификации источников.</summary>
    Task UpdateClassifications(IReadOnlyList<PaymentSource> paymentSources);

    /// <summary>Последние созданные источники без классификации, новые первыми.</summary>
    /// <param name="count">Сколько источников вернуть (не больше).</param>
    Task<List<PaymentSource>> GetLatestUnclassified(int count);

    /// <summary>Есть ли источник с таким именем.</summary>
    Task<bool> Exists(string paymentSourceName);

    /// <summary>Страница источников, новые первыми.</summary>
    /// <param name="search">Подстрока имени; <c>null</c>/пусто — без фильтра.</param>
    /// <param name="noCategoryOnly">Только источники без категории.</param>
    /// <param name="page">Номер страницы с 1.</param>
    /// <param name="pageSize">Размер страницы; <c>null</c> — все записи.</param>
    /// <returns>Записи страницы и общее число записей по фильтру.</returns>
    Task<(List<PaymentSource> Items, int TotalCount)> GetListPaged(string? search, bool noCategoryOnly, int page, int? pageSize);
}
