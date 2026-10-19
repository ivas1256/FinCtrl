using FinCtrl.Common;
using FinCtrl.Domain.Statistic;

namespace FinCtrl.Application.Statistic
{
    public interface IMainStatisticService
    {
        Task<List<MainStatisticGroupItem>> Calculate(DateRange dateRange, GroupingType groupingType, int[]? tagIds = null);
    }
}