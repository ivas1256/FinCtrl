using FinCtrl.Common;
using FinCtrl.Domain.Dashboard;

namespace FinCtrl.Application.Dashboard
{
    public interface IDashboardService
    {
        Task<DashboardSummary> GetSummary();
        Task<List<ChartPoint>> GetChartData(ChartMode mode, DateRange range);
    }
}
