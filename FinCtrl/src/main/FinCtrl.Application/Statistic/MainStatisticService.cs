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
        private readonly Category _transfersCategory = Category.CreateFakeTransfers();

        public MainStatisticService(IPaymentService paymentService, ICategoryService categoryService, TimeProvider timeProvider)
        {
            _paymentService = paymentService;
            _categoryService = categoryService;
            _timeProvider = timeProvider;
        }

        public async Task<PaymentsTable> GetPaymentsTable(DateRange? dateRange, GroupingType groupingType, int[]? tagIds = null)
        {
            var range = dateRange ?? GetDefaultRange();
            var tree = await _categoryService.GetCategoriesTree();
            var savingsIds = GetSavingsCategoryIds(tree);
            var items = await Calculate(range, groupingType, tagIds, savingsIds);

            // П часто лежит вне фильтра дат, поэтому суммы сравнения — отдельной выборкой за [начало П, D + 1).
            var comparison = GetComparison(range, groupingType);
            var comparisonItems = await Calculate(
                new DateRange(comparison.Previous.Range.From, comparison.Current.Range.To), groupingType, tagIds, savingsIds);

            tree.Add(Category.CreateFakeTransfers());
            tree.Add(Category.CreateFakeEmpty());
            tree.Add(Category.CreateFakeTotal());

            var periodRanges = items.Select(x => x.Period).ToList();
            // Т без платежей всё равно получает колонку, если пересекается с фильтром: колонка Δ стоит сразу после неё.
            var currentPeriod = new DateRange(comparison.Current.Range.From, comparison.CurrentPeriodTo);
            if (currentPeriod.From < range.To && range.From < currentPeriod.To)
            {
                periodRanges.Add(currentPeriod);
            }

            var periods = periodRanges
                .Distinct()
                .Order()
                .Select(x => new PaymentsTablePeriod { Key = ToPeriodKey(x, groupingType), Range = x })
                .ToList();

            var context = new TableContext(tree, items, periods, comparisonItems, comparison);
            return new PaymentsTable
            {
                Periods = periods,
                Tree = tree.Select(x => BuildNode(x, context)).ToList(),
                Comparison = comparison
            };
        }

        /// <summary>Суммы платежей по итоговым категориям и периодам группировки плюс итог «ВСЕГО» по периоду.</summary>
        /// <param name="dateRange">Период выборки; <c>To</c> — исключающая граница.</param>
        /// <exception cref="ValidationException">Группировка не выбрана или неизвестна.</exception>
        public async Task<List<MainStatisticGroupItem>> Calculate(DateRange dateRange, GroupingType groupingType, int[]? tagIds = null)
        {
            ValidateGrouping(groupingType);

            var savingsIds = GetSavingsCategoryIds(await _categoryService.GetCategoriesTree());
            return await Calculate(dateRange, groupingType, tagIds, savingsIds);
        }

        private async Task<List<MainStatisticGroupItem>> Calculate(
            DateRange dateRange, GroupingType groupingType, int[]? tagIds, IReadOnlySet<int> savingsIds)
        {
            ValidateGrouping(groupingType);

            var payments = (await _paymentService.GetByDateRange(dateRange, tagIds))
                .Select(x => (Payment: x, Category: GetReportCategory(x, savingsIds)))
                .ToList();

            var fakePaymentsSummary = payments
                .GroupBy(x => GetDateForGrouping(x.Payment.PaymentDate, groupingType))
                .Select(x => new MainStatisticGroupItem()
                {
                    Period = new DateRange(x.Key.Date, GetEndOfPeriod(x.Key.Date, groupingType)),
                    Category = Category.CreateFakeTotal(),
                    TotalSum = x.Where(x => x.Category.IsIncludeInTotalResults())
                                .Sum(y => y.Payment.PaymentSum),
                    Payments = x.Where(x => x.Category.IsIncludeInTotalResults())
                                .Select(x => x.Payment)
                                .OrderByDescending(x => x.PaymentDate)
                                .ToList()
                });

            var result = payments
                .GroupBy(x => new
                {
                    Date = GetDateForGrouping(x.Payment.PaymentDate, groupingType),
                    x.Category,
                })
                .Select(x => new MainStatisticGroupItem()
                {
                    Period = new DateRange(x.Key.Date, GetEndOfPeriod(x.Key.Date, groupingType)),
                    Category = x.Key.Category,
                    TotalSum = x.Sum(y => y.Payment.PaymentSum),
                    Payments = x.Select(y => y.Payment).OrderByDescending(y => y.PaymentDate).ToList()
                })
                .Union(fakePaymentsSummary)
                .OrderBy(x => x.Category?.OrderIndex ?? _nullCategory.OrderIndex)
                .ToList();

            return result;
        }

        // Проверка до выборки: без платежей группировка не вызывается, и ошибка зависела бы от данных.
        private static void ValidateGrouping(GroupingType groupingType)
        {
            if (groupingType == GroupingType.NotSelected || !Enum.IsDefined(groupingType))
            {
                throw new ValidationException("Не выбран тип группировки периодов");
            }
        }

        /// <summary>«Накопления» и все их потомки: перевод с такой категорией остаётся в «Накоплениях».</summary>
        private IReadOnlySet<int> GetSavingsCategoryIds(List<Category> tree)
        {
            var ids = _categoryService.GetAllChildCategories(CategoryIds.Savings, tree).Select(x => x.CategoryId).ToHashSet();
            ids.Add(CategoryIds.Savings);
            return ids;
        }

        /// <summary>
        /// Строка отчёта платежа: перевод между своими счетами — «Переводы», если его итоговая категория не «Накопления»
        /// и не их потомок (в т.ч. перевод без категории или со старой категорией траты/дохода); иначе — итоговая
        /// категория или «Без категории».
        /// </summary>
        private Category GetReportCategory(Payment payment, IReadOnlySet<int> savingsIds)
        {
            var category = payment.CalculatedCategory;
            if (payment.PaymentType == PaymentTypeEnum.Transfer
                && (category is null || !savingsIds.Contains(category.CategoryId)))
            {
                return _transfersCategory;
            }

            return category ?? _nullCategory;
        }

        // Ячейка категории = её собственные платежи + платежи всех потомков; ячейки есть для всех периодов.
        // Суммы сравнения — по тому же правилу, без списков платежей.
        private PaymentsTableNode BuildNode(Category category, TableContext context)
        {
            var descendants = _categoryService.GetAllChildCategories(category.CategoryId, context.Tree);

            decimal SumOf(List<MainStatisticGroupItem> items, DateTime periodStart) => items
                .Where(x => (x.Category.Equals(category) || descendants.Contains(x.Category)) && x.Period.From == periodStart)
                .Sum(x => x.TotalSum);

            var node = new PaymentsTableNode
            {
                Category = category,
                Children = category.Children.Select(x => BuildNode(x, context)).ToList(),
                Comparison = new PaymentsTableNodeComparison
                {
                    Current = SumOf(context.ComparisonItems, context.Comparison.Current.Range.From),
                    Previous = SumOf(context.ComparisonItems, context.Comparison.Previous.Range.From)
                }
            };

            foreach (var period in context.Periods)
            {
                var own = context.Items.FirstOrDefault(x => x.Category.Equals(category) && x.Period.Equals(period.Range));
                var ofDescendants = context.Items
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

        // D = min(сегодня, последний день фильтра); Т — период группировки, содержащий D, по D включительно; П — предыдущий целиком.
        private PaymentsTableComparison GetComparison(DateRange range, GroupingType groupingType)
        {
            var today = _timeProvider.GetLocalNow().Date;
            var lastFilterDay = range.To.AddDays(-1).Date;
            var asOf = today < lastFilterDay ? today : lastFilterDay;

            var currentStart = GetDateForGrouping(asOf, groupingType);
            var currentPeriodTo = GetEndOfPeriod(currentStart, groupingType);
            var current = new DateRange(currentStart, asOf.AddDays(1));
            var previous = new DateRange(GetDateForGrouping(currentStart.AddDays(-1), groupingType), currentStart);

            return new PaymentsTableComparison
            {
                AsOf = asOf,
                Current = new PaymentsTablePeriod { Key = ToPeriodKey(current, groupingType), Range = current },
                Previous = new PaymentsTablePeriod { Key = ToPeriodKey(previous, groupingType), Range = previous },
                CurrentPeriodTo = currentPeriodTo
            };
        }

        private record TableContext(
            List<Category> Tree,
            List<MainStatisticGroupItem> Items,
            List<PaymentsTablePeriod> Periods,
            List<MainStatisticGroupItem> ComparisonItems,
            PaymentsTableComparison Comparison);

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
