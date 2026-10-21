using FinCtrl.Common;
using FinCtrl.Domain.Statistic;

namespace FinCtrl.Application.Statistic
{
    public interface IMainStatisticService
    {
        /// <summary>
        /// Таблица главной: дерево категорий (плюс «Без категории» и итог «ВСЕГО») × периоды группировки.
        /// Сумма и платежи ячейки — категории вместе со всеми её потомками.
        /// </summary>
        /// <param name="dateRange">Период выборки (<c>To</c> — исключающая граница); <c>null</c> — с начала месяца
        /// три месяца назад до конца текущего месяца.</param>
        /// <param name="groupingType">Тип группировки периодов.</param>
        /// <param name="tagIds">Если задан — только платежи хотя бы с одним из тегов.</param>
        /// <exception cref="FinCtrl.Common.Application.ValidationException">Группировка не выбрана или неизвестна.</exception>
        Task<PaymentsTable> GetPaymentsTable(DateRange? dateRange, GroupingType groupingType, int[]? tagIds = null);
    }
}
