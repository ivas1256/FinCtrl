using FinCtrl.Application.Categories;
using FinCtrl.Application.Payments;
using FinCtrl.Common;
using FinCtrl.Domain;
using FinCtrl.Domain.Dashboard;
using System.Globalization;

namespace FinCtrl.Application.Dashboard
{
    internal class DashboardService : IDashboardService
    {
        private readonly IPaymentService _paymentService;
        private readonly ICategoryService _categoryService;
        private readonly TimeProvider _timeProvider;

        public DashboardService(IPaymentService paymentService, ICategoryService categoryService, TimeProvider timeProvider)
        {
            _paymentService = paymentService;
            _categoryService = categoryService;
            _timeProvider = timeProvider;
        }

        public async Task<DashboardSummary> GetSummary()
        {
            var today = _timeProvider.GetLocalNow().Date;
            var yesterday = today.AddDays(-1);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var weekStart = GetWeekStart(today);
            var weekEnd = weekStart.AddDays(6);

            var fetchFrom = new[] { monthStart, weekStart, yesterday }.Min();
            var payments = await _paymentService.GetByDateRange(new DateRange(fetchFrom, today.AddDays(1)));

            var spendings = FilterSpendings(payments);

            var allCategories = await BuildAllCategoriesWithSpending(spendings);

            return new DashboardSummary
            {
                TodayDate = today,
                TodaySpending = SumWhere(spendings, p => p.PaymentDate.Date == today),
                YesterdayDate = yesterday,
                YesterdaySpending = SumWhere(spendings, p => p.PaymentDate.Date == yesterday),
                WeekFrom = weekStart,
                WeekTo = today < weekEnd ? today : weekEnd,
                WeekSpending = SumWhere(spendings, p => p.PaymentDate.Date >= weekStart),
                MonthFrom = monthStart,
                MonthTo = today,
                MonthSpending = SumWhere(spendings, p => p.PaymentDate.Date >= monthStart),
                AllCategories = allCategories,
                TopPayments = spendings
                    .OrderByDescending(p => Math.Abs(p.PaymentSum))
                    .Take(3)
                    .ToList()
            };
        }

        public async Task<List<ChartPoint>> GetChartData(ChartMode mode, DateRange range)
        {
            var payments = await _paymentService.GetByDateRange(
                new DateRange(range.From, range.To.AddDays(1)));

            var spendings = FilterSpendings(payments);

            return mode switch
            {
                ChartMode.Day   => GroupByDay(spendings, range),
                ChartMode.Week  => GroupByWeek(spendings, range),
                ChartMode.Month => GroupByMonth(spendings, range),
                _ => throw new NotImplementedException(mode.ToString())
            };
        }

        // ── Category tree ──────────────────────────────────────────────────

        private async Task<List<CategorySpendingItem>> BuildAllCategoriesWithSpending(List<Payment> spendings)
        {
            var tree = await _categoryService.GetCategoriesTree();

            var spendingById = spendings
                .Where(p => p.CalculatedCategory != null)
                .GroupBy(p => p.CalculatedCategory!.CategoryId)
                .ToDictionary(g => g.Key, g => g.Sum(p => Math.Abs(p.PaymentSum)));

            var flat = FlattenTree(tree, "");

            return flat
                .Select(item => new CategorySpendingItem
                {
                    CategoryId = item.Cat.CategoryId,
                    CategoryName = item.Cat.CategoryName,
                    FullCategoryName = item.FullName,
                    TotalSum = spendingById.GetValueOrDefault(item.Cat.CategoryId, 0)
                })
                .OrderByDescending(x => x.TotalSum)
                .ThenBy(x => x.FullCategoryName)
                .ToList();
        }

        private static List<(Category Cat, string FullName)> FlattenTree(
            IEnumerable<Category> categories, string parentPath)
        {
            var result = new List<(Category, string)>();
            foreach (var cat in categories)
            {
                if (cat.CategoryId == int.MaxValue || cat.CategoryId == int.MaxValue - 1)
                    continue;

                var fullName = string.IsNullOrEmpty(parentPath)
                    ? cat.CategoryName
                    : $"{parentPath} / {cat.CategoryName}";

                result.Add((cat, fullName));

                if (cat.Children?.Count > 0)
                    result.AddRange(FlattenTree(cat.Children, fullName));
            }
            return result;
        }

        // ── Chart grouping ─────────────────────────────────────────────────

        private static List<ChartPoint> GroupByDay(List<Payment> spendings, DateRange range)
        {
            var byDate = spendings
                .GroupBy(p => p.PaymentDate.Date)
                .ToDictionary(g => g.Key, g => g.Sum(p => Math.Abs(p.PaymentSum)));

            var days = (range.To - range.From).Days + 1;
            return Enumerable.Range(0, days)
                .Select(d =>
                {
                    var date = range.From.AddDays(d);
                    return new ChartPoint { Label = date.ToString("dd.MM"), Value = byDate.GetValueOrDefault(date, 0) };
                })
                .ToList();
        }

        private static List<ChartPoint> GroupByWeek(List<Payment> spendings, DateRange range)
        {
            var weeks = new List<DateTime>();
            var cur = GetWeekStart(range.From);
            while (cur <= range.To) { weeks.Add(cur); cur = cur.AddDays(7); }

            return weeks.Select(ws =>
            {
                var we = ws.AddDays(6);
                var displayEnd = we > range.To ? range.To : we;
                var sum = spendings
                    .Where(p => p.PaymentDate.Date >= ws && p.PaymentDate.Date <= we)
                    .Sum(p => Math.Abs(p.PaymentSum));
                return new ChartPoint { Label = $"{ws:dd.MM}–{displayEnd:dd.MM}", Value = sum };
            }).ToList();
        }

        private static List<ChartPoint> GroupByMonth(List<Payment> spendings, DateRange range)
        {
            var months = new List<DateTime>();
            var cur = new DateTime(range.From.Year, range.From.Month, 1);
            while (cur <= range.To) { months.Add(cur); cur = cur.AddMonths(1); }

            return months.Select(m =>
            {
                var nextM = m.AddMonths(1);
                var sum = spendings
                    .Where(p => p.PaymentDate.Date >= m && p.PaymentDate.Date < nextM)
                    .Sum(p => Math.Abs(p.PaymentSum));
                return new ChartPoint { Label = m.ToString("MMM yy", new CultureInfo("ru-RU")), Value = sum };
            }).ToList();
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static List<Payment> FilterSpendings(List<Payment> payments)
            => payments
                .Where(p => p.PaymentSum < 0
                    && (p.CalculatedCategory?.IsIncludeInTotalResults() ?? false))
                .ToList();

        private static decimal SumWhere(List<Payment> payments, Func<Payment, bool> predicate)
            => payments.Where(predicate).Sum(p => Math.Abs(p.PaymentSum));

        private static DateTime GetWeekStart(DateTime date)
            => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)).Date;
    }
}
