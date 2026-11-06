using FinCtrl.Common;
using FinCtrl.Domain;

namespace FinCtrl.Application.Payments;

/// <summary>Хранилище платежей. Удалённые (<c>IsDeleted</c>) платежи в выборки не попадают.</summary>
public interface IPaymentRepository
{
    /// <summary>Платежи за период для статистики (без исключённых из статистики), с тегами.</summary>
    /// <param name="dateRange">Период; <c>To</c> — исключающая граница.</param>
    /// <param name="tagIds">Если задан — только платежи хотя бы с одним из тегов.</param>
    Task<List<Payment>> GetByDateRange(DateRange dateRange, int[]? tagIds = null);

    /// <summary>Платёж по id (без тегов) или <c>null</c>, если не найден.</summary>
    Task<Payment?> Get(int id);

    /// <summary>Создаёт платёж.</summary>
    /// <returns>Id созданного платежа.</returns>
    Task<int> Create(Payment payment);

    /// <summary>Сохраняет изменения платежей (без тегов).</summary>
    Task Update(params Payment[] payments);

    /// <summary>
    /// Неудалённые платежи с такой суммой (как хранится, без нормализации знака) и датой, по возрастанию id —
    /// кандидаты в дубли при импорте.
    /// </summary>
    Task<List<Payment>> GetByDateAndSum(decimal paymentSum, DateTime paymentDate);

    /// <summary>Платежи источника.</summary>
    Task<List<Payment>> GetBySource(int PaymentSourceId);

    /// <summary>Страница платежей для списка операций, с тегами.</summary>
    /// <param name="from">Начало периода (включительно); <c>null</c> — без нижней границы.</param>
    /// <param name="to">Конец периода — исключающая граница; <c>null</c> — без верхней границы.</param>
    /// <param name="sourceId">Фильтр по источнику; <c>null</c> — все.</param>
    /// <param name="noCategoryOnly">Только платежи без категории (ни своей, ни у источника).</param>
    /// <param name="sortBy"><c>source</c>, <c>category</c>, иначе — по дате.</param>
    /// <param name="sortDir"><c>asc</c>, иначе — по убыванию.</param>
    /// <param name="page">Номер страницы с 1.</param>
    /// <param name="pageSize">Размер страницы; <c>null</c> — все записи.</param>
    /// <param name="tagIds">Если задан — только платежи хотя бы с одним из тегов.</param>
    /// <returns>Записи страницы и общее число записей по фильтру.</returns>
    Task<(List<Payment> Items, int TotalCount)> GetListPaged(DateTime? from, DateTime? to, int? sourceId, bool noCategoryOnly, string sortBy, string sortDir, int page, int? pageSize, int[]? tagIds = null);

    /// <summary>Заменяет теги платежа целиком (пустой список — снять все).</summary>
    Task SetPaymentTags(int paymentId, IReadOnlyCollection<int> tagIds);

    /// <summary>Добавляет теги платежам, не трогая уже назначенные.</summary>
    Task AddTagsToPayments(IEnumerable<int> paymentIds, IEnumerable<int> tagIds);
}
