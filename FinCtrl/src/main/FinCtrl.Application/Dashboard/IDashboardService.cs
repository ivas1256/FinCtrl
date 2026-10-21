using FinCtrl.Domain.Dashboard;

namespace FinCtrl.Application.Dashboard
{
    public interface IDashboardService
    {
        /// <summary>
        /// Траты за сегодня, вчера, неделю и месяц (по текущей дате <see cref="TimeProvider"/>), траты по категориям
        /// с долей от трат месяца и три крупнейшие траты. Учитываются только расходы категорий, входящих в итоги.
        /// </summary>
        Task<DashboardSummary> GetSummary();

        /// <summary>Траты по дням, неделям или месяцам для графика.</summary>
        /// <param name="mode">Шаг графика.</param>
        /// <param name="from">Начало периода; <c>null</c> — по умолчанию для режима: 29 дней назад (день),
        /// понедельник три недели назад (неделя), 1 января (месяц).</param>
        /// <param name="to">Конец периода — исключающая граница; <c>null</c> — завтра (график включает сегодня).</param>
        /// <exception cref="FinCtrl.Common.Application.ValidationException">Неизвестный режим или начало не раньше конца.</exception>
        Task<List<ChartPoint>> GetChartData(ChartMode mode, DateTime? from = null, DateTime? to = null);
    }
}
