using FinCtrl.Common;
using FinCtrl.Domain.Statistic;

namespace FinCtrl.Application.Statistic
{
    public interface IMainStatisticService
    {
        /// <summary>Суммы платежей по категориям и периодам группировки (плюс итог по периоду).</summary>
        /// <param name="dateRange">Период выборки; <c>To</c> — исключающая граница.</param>
        /// <param name="groupingType">Тип группировки периодов.</param>
        /// <param name="tagIds">Если задан — только платежи хотя бы с одним из тегов.</param>
        /// <exception cref="FinCtrl.Common.Application.ValidationException">Группировка не выбрана или неизвестна.</exception>
        Task<List<MainStatisticGroupItem>> Calculate(DateRange dateRange, GroupingType groupingType, int[]? tagIds = null);
    }
}
