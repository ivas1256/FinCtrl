using System.Globalization;
using FinCtrl.Application.Categories;
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
        private readonly ICategoryService _categoryService;
        private readonly TimeProvider _timeProvider;
        private readonly Category _nullCategory = Category.CreateFakeEmpty();

        public MainStatisticService(IPaymentService paymentService, ICategoryService categoryService, TimeProvider timeProvider)
        {
            _paymentService = paymentService;
            _categoryService = categoryService;
            _timeProvider = timeProvider;
        }

        public async Task<PaymentsTable> GetPaymentsTable(DateRange? dateRange, GroupingType groupingType, int[]? tagIds = null)
        {
            var items = await Calculate(dateRange ?? GetDefaultRange(), groupingType, tagIds);

            var tree = await _categoryService.GetCategoriesTree();
            tree.Add(Category.CreateFakeEmpty());
            tree.Add(Category.CreateFakeTotal());

            var periods = items.Select(x => x.Period)
                .Distinct()
                .Order()
                .Select(x => new PaymentsTablePeriod { Key = ToPeriodKey(x, groupingType), Range = x })
                .ToList();

            return new PaymentsTable
            {
                Periods = periods,
                Tree = tree.Select(x => BuildNode(x, tree, items, periods)).ToList()
            };
        }

        /// <summary>Суммы платежей по итоговым категориям и периодам группировки плюс итог «ВСЕГО» по периоду.</summary>
        /// <param name="dateRange">Период выборки; <c>To</c> — исключающая граница.</param>
        /// <exception cref="ValidationException">Группировка не выбрана или неизвестна.</exception>
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
                    Payments = x.OrderByDescending(x => x.PaymentDate).ToList()
                })
                .Union(fakePaymentsSummary)
                .OrderBy(x => x.Category?.OrderIndex ?? _nullCategory.OrderIndex)
                .ToList();

            return result;
        }

        // Ячейка категории = её собственные платежи + платежи всех потомков; ячейки есть для всех периодов.
        private PaymentsTableNode BuildNode(Category category, List<Category> tree, List<MainStatisticGroupItem> items, List<PaymentsTablePeriod> periods)
        {
            var descendants = _categoryService.GetAllChildCategories(category.CategoryId, tree);

            var node = new PaymentsTableNode
            {
                Category = category,
                Children = category.Children.Select(x => BuildNode(x, tree, items, periods)).ToList()
            };

            foreach (var period in periods)
            {
                var own = items.FirstOrDefault(x => x.Category.Equals(category) && x.Period.Equals(period.Range));
                var ofDescendants = items
                    .Where(x => descendants.Contains(x.Category) && x.Period.Equals(period.Range))
                    .ToList();

                var payments = ofDescendants.SelectMany(x => x.Payments);
                if (own is not null)
                {
                    payments = payments.Union(own.Payments);
                }

                node.Cells[period.Key] = new PaymentsTableCell
                {
                    TotalSum = (own?.TotalSum ?? 0) + ofDescendants.Sum(x => x.TotalSum),
                    Payments = payments.ToList()
                };
            }

            return node;
        }

        private DateRange GetDefaultRange()
        {
            var today = _timeProvider.GetLocalNow().Date;
            return new DateRange(today.AddMonths(-3).FirstDayOfMonth(), today.NextMonthStart());
        }

        private static string ToPeriodKey(DateRange period, GroupingType groupingType)
        {
            var from = period.From;
            return groupingType switch
            {
                GroupingType.Month => from.ToString("MMyyyy", CultureInfo.InvariantCulture),
                GroupingType.Week => GetWeekNumber(from).ToString(CultureInfo.InvariantCulture) + from.ToString("yyyy", CultureInfo.InvariantCulture),
                GroupingType.Quartal => $"Q{(from.Month - 1) / 3 + 1}{from.ToString("yyyy", CultureInfo.InvariantCulture)}",
                GroupingType.Year => from.ToString("yyyy", CultureInfo.InvariantCulture),
                GroupingType.Day => from.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                _ => throw new ArgumentOutOfRangeException(nameof(groupingType), groupingType, null)
            };
        }

        private static int GetWeekNumber(DateTime date)
            => CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

        private static DateTime GetDateForGrouping(DateTime paymentDate, GroupingType groupingType)
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

        // Конец периода — исключающая граница (как у всех DateRange).
        private static DateTime GetEndOfPeriod(DateTime periodStartDate, GroupingType groupingType)
        {
            return groupingType switch
            {
                GroupingType.Month => periodStartDate.AddMonths(1),
                GroupingType.Quartal => periodStartDate.AddMonths(3),
                GroupingType.Week => periodStartDate.AddDays(7).Date,
                GroupingType.Day => periodStartDate.Date.AddDays(1),
                GroupingType.Year => new DateTime(periodStartDate.Year + 1, 1, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(groupingType), groupingType, null),
            };
        }
    }
}
