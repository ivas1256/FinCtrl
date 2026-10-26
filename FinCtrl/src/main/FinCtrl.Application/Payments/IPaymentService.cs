using FinCtrl.Common;
using FinCtrl.Domain;

namespace FinCtrl.Application.Payments
{
    public interface IPaymentService
    {
        /// <summary>Платежи за период для статистики (без исключённых из статистики), с тегами.</summary>
        /// <param name="dateRange">Период; <c>To</c> — исключающая граница.</param>
        /// <param name="tagIds">Если задан — только платежи хотя бы с одним из тегов.</param>
        Task<List<Payment>> GetByDateRange(DateRange dateRange, int[]? tagIds = null);

        /// <summary>Платёж по id (без тегов).</summary>
        /// <exception cref="FinCtrl.Common.Application.NotFoundException">Платёж не найден.</exception>
        Task<Payment> Get(int id);

        /// <summary>Создаёт платёж.</summary>
        /// <returns>Id созданного платежа.</returns>
        Task<int> Create(Payment payment);

        /// <summary>Сохраняет изменения платежей (без тегов).</summary>
        /// <exception cref="FinCtrl.Common.Application.ValidationException">Указанная категория или счёт не найдены.</exception>
        Task Update(params Payment[] payments);

        /// <summary>
        /// Правит платёж (поля и теги) одной транзакцией; поля с <c>null</c> не меняются (см. <see cref="PaymentEdit"/>).
        /// Связанные категория, счёт и теги проверяются до записи.
        /// </summary>
        /// <exception cref="FinCtrl.Common.Application.NotFoundException">Платёж не найден.</exception>
        /// <exception cref="FinCtrl.Common.Application.ValidationException">Категория, счёт или какой-то из тегов не найдены.</exception>
        Task Edit(PaymentEdit edit);

        /// <summary>Платёж с такой же суммой и датой (поиск дубля при импорте) или <c>null</c>.</summary>
        Task<Payment?> FindDuplicate(decimal paymentSum, DateTime paymentDate);

        /// <summary>Платежи источника; пустой список, если источника нет.</summary>
        Task<List<Payment>> GetBySource(int PaymentSourceId);

        /// <summary>Страница платежей для списка операций, с тегами (параметры — как у <see cref="IPaymentRepository.GetListPaged"/>).</summary>
        /// <exception cref="FinCtrl.Common.Application.ValidationException"><paramref name="page"/> или <paramref name="pageSize"/> меньше 1.</exception>
        Task<(List<Payment> Items, int TotalCount)> GetListPaged(DateTime? from, DateTime? to, int? sourceId, bool noCategoryOnly, string sortBy, string sortDir, int page, int? pageSize, int[]? tagIds = null);

        /// <summary>Заменяет теги платежа целиком (пустой список — снять все).</summary>
        /// <exception cref="FinCtrl.Common.Application.ValidationException">Какой-то из тегов не найден.</exception>
        Task SetPaymentTags(int paymentId, IReadOnlyCollection<int> tagIds);

        /// <summary>Добавляет теги платежам периода с итоговой категорией из списка, не трогая уже назначенные.</summary>
        /// <param name="dateRange">Период; <c>To</c> — исключающая граница.</param>
        /// <param name="categoryIds">Итоговые категории (без учёта потомков); пустой список — все платежи периода, включая без категории.</param>
        /// <returns>Число подходящих платежей.</returns>
        /// <exception cref="FinCtrl.Common.Application.ValidationException">Не выбраны теги, тег не найден или период перевёрнут.</exception>
        Task<int> BulkAssignTags(DateRange dateRange, List<int> categoryIds, List<int> tagIds);
    }
}
