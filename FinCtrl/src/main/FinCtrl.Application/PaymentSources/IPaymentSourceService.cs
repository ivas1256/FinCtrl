using FinCtrl.Domain;

namespace FinCtrl.Application.PaymentSources
{
    public interface IPaymentSourceService
    {
        /// <summary>Все источники с категориями, новые первыми.</summary>
        Task<List<PaymentSource>> GetAll();

        /// <summary>Источник по id.</summary>
        /// <exception cref="FinCtrl.Common.Application.NotFoundException">Источник не найден.</exception>
        Task<PaymentSource> GetById(int paymentSourceId);

        /// <summary>Источник по имени или <c>null</c>, если его нет (get-or-create при импорте).</summary>
        Task<PaymentSource?> GetByName(string paymentSourceName);

        /// <summary>Создаёт источник.</summary>
        /// <returns>Id созданного источника.</returns>
        Task<int> Create(PaymentSource paymentSource);

        /// <summary>
        /// Правит источник платежа: имя, категорию, описание и классификацию.
        /// </summary>
        /// <param name="categoryId">Категория источника; <c>null</c> — снять категорию.</param>
        /// <param name="classification">
        /// Классификация; <c>null</c>/пустая строка — очистить. Если значение изменилось, признак ИИ-классификации снимается.
        /// </param>
        /// <exception cref="FinCtrl.Common.Application.NotFoundException">Источник не найден.</exception>
        /// <exception cref="FinCtrl.Common.Application.ValidationException">Имя не заполнено или категория не найдена.</exception>
        Task Edit(int paymentSourceId, string paymentSourceName, int? categoryId, string? description, string? classification);

        /// <summary>Страница источников, новые первыми.</summary>
        /// <param name="search">Подстрока имени; <c>null</c>/пусто — без фильтра.</param>
        /// <param name="noCategoryOnly">Только источники без категории.</param>
        /// <param name="page">Номер страницы с 1.</param>
        /// <param name="pageSize">Размер страницы; <c>null</c> — все записи.</param>
        /// <exception cref="FinCtrl.Common.Application.ValidationException"><paramref name="page"/> или <paramref name="pageSize"/> меньше 1.</exception>
        Task<(List<PaymentSource> Items, int TotalCount)> GetListPaged(string? search, bool noCategoryOnly, int page, int? pageSize);
    }
}
