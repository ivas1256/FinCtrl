using FinCtrl.Application.Payments;
using FinCtrl.Common;
using FinCtrl.Common.Application;
using FinCtrl.Domain;
using FinCtrl.Domain.Statistic;

namespace FinCtrl.Application.Statistic
{
    internal class MainStatisticService : IMainStatisticService
    {
        private readonly IPaymentService _paymentService;
        private readonly Category _nullCategory = Category.CreateFakeEmpty();

        public MainStatisticService(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        public async Task<List<MainStatisticGroupItem>> Calculate(DateRange dateRange, GroupingType groupingType, int[]? tagIds = null)
        {
            // Проверка до выборки: без платежей группировка не вызывается, и ошибка зависела бы от данных.
            if (groupingType == GroupingType.NotSelected || !Enum.IsDefined(groupingType))
            {
                throw new ValidationException("Не выбран тип группировки периодов");
            }

            var payments = await _paymentService.GetByDateRange(dateRange, tagIds);

            var fakePaymentsSummary = payments
                .GroupBy(x => GetDateForGrouping(x.PaymentDate, groupingType))
                .Select(x => new MainStatisticGroupItem()
                {
                    Period = new DateRange(x.Key.Date, GetEndOfPeriod(x.Key.Date, groupingType)),
                    Category = Category.CreateFakeTotal(),
                    TotalSum = x.Where(x => x.CalculatedCategory?.IsIncludeInTotalResults() ?? false)
                                .Sum(y => y.PaymentSum),
                    Payments = x.Where(x => x.CalculatedCategory?.IsIncludeInTotalResults() ?? false)
                                .OrderByDescending(x => x.PaymentDate)
                                .Select(x => MainStatisticPayment.FromPayment(x))
                                .ToList()
                });

            var result = payments
                .GroupBy(x => new
                {
                    Date = GetDateForGrouping(x.PaymentDate, groupingType),
                    Category = x.CalculatedCategory ?? _nullCategory,
                })
                .Select(x => new MainStatisticGroupItem()
                {
                    Period = new DateRange(x.Key.Date, GetEndOfPeriod(x.Key.Date, groupingType)),
                    Category = x.Key.Category,
                    TotalSum = x.Sum(y => y.PaymentSum),
                    Payments = x.OrderByDescending(x => x.PaymentDate)
                                .Select(x => MainStatisticPayment.FromPayment(x))
                                .ToList()
                })
                .Union(fakePaymentsSummary)
                .OrderBy(x => x.Category?.OrderIndex ?? _nullCategory.OrderIndex)
                .ToList();

            return result;
        }

        private DateTime GetDateForGrouping(DateTime paymentDate, GroupingType groupingType)
        {
            switch (groupingType)
            {
                case GroupingType.Month:
                    return new DateTime(paymentDate.Year, paymentDate.Month, 1);
                case GroupingType.Quartal:
                    int quarterStartMonth = (paymentDate.Month - 1) / 3 * 3 + 1;
                    return new DateTime(paymentDate.Year, quarterStartMonth, 1);
                case GroupingType.Week:
                    int diff = (7 + (paymentDate.DayOfWeek - DayOfWeek.Monday)) % 7;
                    return paymentDate.AddDays(-diff).Date;
                case GroupingType.Day:
                    return paymentDate.Date;
                case GroupingType.Year:
                    return new DateTime(paymentDate.Year, 1, 1);
                default:
                    throw new ArgumentOutOfRangeException(nameof(groupingType), groupingType, null);
            }
        }

        private DateTime GetEndOfPeriod(DateTime periodStartDate, GroupingType groupingType)
        {
            return groupingType switch
            {
                GroupingType.Month => periodStartDate.AddMonths(1),
                GroupingType.Quartal => periodStartDate.AddMonths(3),
                GroupingType.Week => periodStartDate.AddDays(7).Date,
                GroupingType.Day => periodStartDate.Date,
                GroupingType.Year => new DateTime(periodStartDate.Year + 1, 1, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(groupingType), groupingType, null),
            };
        }
    }
}
