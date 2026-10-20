using FinCtrl.Application.Categories;
using FinCtrl.Application.Payments;
using FinCtrl.Application.Statistic;
using FinCtrl.Common;
using FinCtrl.Common.Application;
using FinCtrl.Domain;
using FinCtrl.Domain.Statistic;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FinCtrl.Application.Tests.Statistic;

public class MainStatisticServiceTests
{
    private static readonly DateRange AnyRange = new(new DateTime(2025, 1, 1), new DateTime(2027, 1, 1));

    private readonly IPaymentService _paymentService = Substitute.For<IPaymentService>();
    private readonly ICategoryService _categoryService = Substitute.For<ICategoryService>();
    private readonly MainStatisticService _service;
    private readonly Category _food = new(1);

    public MainStatisticServiceTests()
    {
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetLocalTimeZone(TimeZoneInfo.Utc);
        timeProvider.SetUtcNow(new DateTimeOffset(2025, 10, 15, 14, 0, 0, TimeSpan.Zero));

        _categoryService.GetCategoriesTree().Returns(_ => BuildTree());
        _categoryService.GetAllChildCategories(Arg.Any<int>(), Arg.Any<List<Category>>())
            .Returns(ci => Descendants(ci.ArgAt<List<Category>>(1), ci.ArgAt<int>(0)));

        _service = new MainStatisticService(_paymentService, _categoryService, timeProvider);
    }

    [Fact]
    public async Task Calculate_Quarter_PaymentsOfOneQuarterGroupedIntoOnePeriod()
    {
        GivenPayments(
            Spending(-100, new DateTime(2026, 1, 5)),
            Spending(-50, new DateTime(2026, 2, 17)),
            Spending(-30, new DateTime(2026, 3, 31, 23, 59, 0)));

        var result = await _service.Calculate(AnyRange, GroupingType.Quartal);

        var item = Assert.Single(result, x => x.Category == _food);
        Assert.Equal(new DateTime(2026, 1, 1), item.Period.From);
        Assert.Equal(new DateTime(2026, 4, 1), item.Period.To);
        Assert.Equal(-180, item.TotalSum);
        Assert.Equal(3, item.Payments.Count);
    }

    [Fact]
    public async Task Calculate_Quarter_PaymentsOfDifferentQuartersGroupedSeparately()
    {
        GivenPayments(
            Spending(-10, new DateTime(2025, 7, 1)),
            Spending(-20, new DateTime(2025, 9, 30)),
            Spending(-40, new DateTime(2025, 10, 1)),
            Spending(-80, new DateTime(2025, 12, 31)),
            Spending(-160, new DateTime(2026, 4, 15)));

        var result = await _service.Calculate(AnyRange, GroupingType.Quartal);

        var periods = result
            .Where(x => x.Category == _food)
            .OrderBy(x => x.Period.From)
            .Select(x => (x.Period.From, x.Period.To, x.TotalSum))
            .ToList();
        Assert.Equal(new[]
        {
            (new DateTime(2025, 7, 1), new DateTime(2025, 10, 1), -30m),
            (new DateTime(2025, 10, 1), new DateTime(2026, 1, 1), -120m),
            (new DateTime(2026, 4, 1), new DateTime(2026, 7, 1), -160m),
        }, periods);
    }

    [Fact]
    public async Task Calculate_Month_PeriodIsCalendarMonth()
    {
        GivenPayments(Spending(-100, new DateTime(2026, 2, 17)));

        var result = await _service.Calculate(AnyRange, GroupingType.Month);

        var item = Assert.Single(result, x => x.Category == _food);
        Assert.Equal(new DateTime(2026, 2, 1), item.Period.From);
        Assert.Equal(new DateTime(2026, 3, 1), item.Period.To);
    }

    [Theory]
    [InlineData(GroupingType.NotSelected)]
    [InlineData((GroupingType)42)]
    public async Task Calculate_GroupingNotSelectedOrUnknown_ThrowsValidationException(GroupingType groupingType)
    {
        GivenPayments(Spending(-100, new DateTime(2026, 2, 17)));

        await Assert.ThrowsAsync<ValidationException>(() => _service.Calculate(AnyRange, groupingType));
    }

    [Fact]
    public async Task Calculate_GroupingNotSelectedWithoutPayments_ThrowsValidationException()
    {
        GivenPayments();

        await Assert.ThrowsAsync<ValidationException>(() => _service.Calculate(AnyRange, GroupingType.NotSelected));
    }

    [Fact]
    public async Task GetPaymentsTable_Tree_NodeSumIncludesDescendants()
    {
        var cafe = new Category(2);
        GivenPayments(
            Spending(-100, new DateTime(2026, 1, 5)),
            Spending(-50, new DateTime(2026, 1, 10), cafe),
            Spending(-30, new DateTime(2026, 2, 1), cafe),
            Spending(-7, new DateTime(2026, 1, 20), category: null));

        var table = await _service.GetPaymentsTable(AnyRange, GroupingType.Month);

        Assert.Equal(new[] { "012026", "022026" }, table.Periods.Select(p => p.Key));
        Assert.Equal(new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 2, 1)), table.Periods[0].Range);
        Assert.Equal(new[] { 1, 3, CategoryIds.FakeEmpty, CategoryIds.FakeTotal }, table.Tree.Select(n => n.Category.CategoryId));

        var food = table.Tree[0];
        Assert.Equal(-150, food.Cells["012026"].TotalSum);
        Assert.Equal(2, food.Cells["012026"].Payments.Count);
        Assert.Equal(-30, food.Cells["022026"].TotalSum);
        Assert.Equal(-50, Assert.Single(food.Children).Cells["012026"].TotalSum);

        var salary = table.Tree[1];
        Assert.Equal(new[] { "012026", "022026" }, salary.Cells.Keys);
        Assert.All(salary.Cells.Values, c => Assert.Equal(0, c.TotalSum));

        Assert.Equal(-7, table.Tree[2].Cells["012026"].TotalSum);
        Assert.Equal(-150, table.Tree[3].Cells["012026"].TotalSum);
    }

    [Theory]
    [InlineData(GroupingType.Month, "2026-02-17", "022026")]
    [InlineData(GroupingType.Week, "2026-01-07", "22026")]
    [InlineData(GroupingType.Quartal, "2026-05-10", "Q22026")]
    [InlineData(GroupingType.Year, "2026-05-10", "2026")]
    [InlineData(GroupingType.Day, "2026-02-17", "17.02.2026")]
    public async Task GetPaymentsTable_Grouping_BuildsPeriodKey(GroupingType groupingType, string date, string expectedKey)
    {
        GivenPayments(Spending(-100, DateTime.Parse(date, System.Globalization.CultureInfo.InvariantCulture)));

        var table = await _service.GetPaymentsTable(AnyRange, groupingType);

        Assert.Equal(expectedKey, Assert.Single(table.Periods).Key);
    }

    [Fact]
    public async Task GetPaymentsTable_Day_PeriodEndIsNextDay()
    {
        GivenPayments(Spending(-100, new DateTime(2026, 2, 17, 15, 0, 0)));

        var table = await _service.GetPaymentsTable(AnyRange, GroupingType.Day);

        Assert.Equal(new DateRange(new DateTime(2026, 2, 17), new DateTime(2026, 2, 18)), Assert.Single(table.Periods).Range);
    }

    [Fact]
    public async Task GetPaymentsTable_NoRange_UsesThreeMonthsBeforeTodayUntilMonthEnd()
    {
        GivenPayments();

        await _service.GetPaymentsTable(null, GroupingType.Month);

        await _paymentService.Received(1).GetByDateRange(
            new DateRange(new DateTime(2025, 7, 1), new DateTime(2025, 11, 1)), Arg.Any<int[]?>());
    }

    [Fact]
    public async Task GetPaymentsTable_NoPayments_ReturnsTreeWithoutPeriods()
    {
        GivenPayments();

        var table = await _service.GetPaymentsTable(AnyRange, GroupingType.Month);

        Assert.Empty(table.Periods);
        Assert.Equal(4, table.Tree.Count);
        Assert.All(table.Tree, n => Assert.Empty(n.Cells));
    }

    [Fact]
    public async Task Calculate_Week_PeriodStartsOnMondayAndSundayBelongsToPreviousWeek()
    {
        GivenPayments(
            Spending(-10, new DateTime(2026, 1, 4, 23, 0, 0)),  // воскресенье
            Spending(-20, new DateTime(2026, 1, 5)),            // понедельник
            Spending(-40, new DateTime(2026, 1, 11, 18, 0, 0))); // воскресенье той же недели

        var result = await _service.Calculate(AnyRange, GroupingType.Week);

        var periods = result
            .Where(x => x.Category == _food)
            .OrderBy(x => x.Period.From)
            .Select(x => (x.Period.From, x.Period.To, x.TotalSum))
            .ToList();
        Assert.Equal(new[]
        {
            (new DateTime(2025, 12, 29), new DateTime(2026, 1, 5), -10m),
            (new DateTime(2026, 1, 5), new DateTime(2026, 1, 12), -60m),
        }, periods);
    }

    [Fact]
    public async Task Calculate_Day_PaymentsOfOneDayGroupedIntoOnePeriod()
    {
        GivenPayments(
            Spending(-10, new DateTime(2026, 2, 17, 0, 0, 0)),
            Spending(-20, new DateTime(2026, 2, 17, 23, 59, 0)),
            Spending(-40, new DateTime(2026, 2, 18, 0, 0, 0)));

        var result = await _service.Calculate(AnyRange, GroupingType.Day);

        var periods = result
            .Where(x => x.Category == _food)
            .OrderBy(x => x.Period.From)
            .Select(x => (x.Period.From, x.Period.To, x.TotalSum))
            .ToList();
        Assert.Equal(new[]
        {
            (new DateTime(2026, 2, 17), new DateTime(2026, 2, 18), -30m),
            (new DateTime(2026, 2, 18), new DateTime(2026, 2, 19), -40m),
        }, periods);
    }

    [Fact]
    public async Task Calculate_Year_PeriodIsCalendarYear()
    {
        GivenPayments(
            Spending(-10, new DateTime(2025, 1, 1)),
            Spending(-20, new DateTime(2025, 12, 31, 23, 59, 0)));

        var result = await _service.Calculate(AnyRange, GroupingType.Year);

        var item = Assert.Single(result, x => x.Category == _food);
        Assert.Equal(new DateRange(new DateTime(2025, 1, 1), new DateTime(2026, 1, 1)), item.Period);
        Assert.Equal(-30, item.TotalSum);
    }

    [Fact]
    public async Task Calculate_NoOwnCategory_GroupsBySourceCategory()
    {
        var cafe = new Category(2) { OrderIndex = 2 };
        GivenPayments(new Payment
        {
            PaymentSum = -50,
            PaymentDate = new DateTime(2026, 1, 10),
            PaymentSource = new PaymentSource(5, "Кофейня", null, null, default, null) { PaymentSourceCategory = cafe }
        });

        var result = await _service.Calculate(AnyRange, GroupingType.Month);

        Assert.Equal(-50, Assert.Single(result, x => x.Category.Equals(cafe)).TotalSum);
        Assert.DoesNotContain(result, x => x.Category.CategoryId == CategoryIds.FakeEmpty);
    }

    [Fact]
    public async Task Calculate_OwnAndSourceCategory_OwnCategoryWins()
    {
        GivenPayments(new Payment
        {
            PaymentSum = -50,
            PaymentDate = new DateTime(2026, 1, 10),
            PaymentCategory = _food,
            PaymentSource = new PaymentSource(5, "Кофейня", null, null, default, null) { PaymentSourceCategory = new Category(2) }
        });

        var result = await _service.Calculate(AnyRange, GroupingType.Month);

        Assert.Equal(-50, Assert.Single(result, x => x.Category == _food).TotalSum);
        Assert.DoesNotContain(result, x => x.Category.CategoryId == 2);
    }

    [Fact]
    public async Task Calculate_NoCategory_GroupedIntoFakeEmptyCategory()
    {
        GivenPayments(Spending(-7, new DateTime(2026, 1, 20), category: null));

        var result = await _service.Calculate(AnyRange, GroupingType.Month);

        var empty = Assert.Single(result, x => x.Category.CategoryId == CategoryIds.FakeEmpty);
        Assert.Equal("Без категории", empty.Category.CategoryName);
        Assert.Equal(-7, empty.TotalSum);
    }

    [Fact]
    public async Task Calculate_Total_IncludesOnlySpendingCategories()
    {
        var salary = new Category(3) { IsOnlyIncome = true, OrderIndex = 3 };
        var savings = new Category(CategoryIds.Savings) { OrderIndex = 4 };
        GivenPayments(
            Spending(-100, new DateTime(2026, 1, 5)),
            Spending(-50, new DateTime(2026, 1, 6), new Category(2) { OrderIndex = 2 }),
            Spending(500, new DateTime(2026, 1, 7), salary),
            Spending(-1000, new DateTime(2026, 1, 8), savings),
            Spending(-7, new DateTime(2026, 1, 9), category: null));

        var result = await _service.Calculate(AnyRange, GroupingType.Month);

        var total = Assert.Single(result, x => x.Category.CategoryId == CategoryIds.FakeTotal);
        Assert.Equal(new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 2, 1)), total.Period);
        Assert.Equal(-150, total.TotalSum);
        Assert.Equal(new[] { new DateTime(2026, 1, 6), new DateTime(2026, 1, 5) }, total.Payments.Select(p => p.PaymentDate));
        Assert.Equal(500, Assert.Single(result, x => x.Category == salary).TotalSum);
        Assert.Equal(-1000, Assert.Single(result, x => x.Category == savings).TotalSum);
    }

    [Fact]
    public async Task Calculate_Result_OrderedByCategoryOrderIndexWithFakeCategoriesLast()
    {
        GivenPayments(
            Spending(-7, new DateTime(2026, 1, 9), category: null),
            Spending(-10, new DateTime(2026, 1, 5), new Category(10) { OrderIndex = 5 }),
            Spending(-20, new DateTime(2026, 1, 5), new Category(11) { OrderIndex = 1 }));

        var result = await _service.Calculate(AnyRange, GroupingType.Month);

        Assert.Equal(new[] { 11, 10, CategoryIds.FakeEmpty, CategoryIds.FakeTotal }, result.Select(x => x.Category.CategoryId));
    }

    [Fact]
    public async Task Calculate_Payments_OrderedByDateDescending()
    {
        GivenPayments(
            Spending(-10, new DateTime(2026, 1, 5)),
            Spending(-20, new DateTime(2026, 1, 20)),
            Spending(-30, new DateTime(2026, 1, 10)));

        var result = await _service.Calculate(AnyRange, GroupingType.Month);

        Assert.Equal(new[] { -20m, -30m, -10m }, Assert.Single(result, x => x.Category == _food).Payments.Select(p => p.PaymentSum));
    }

    [Fact]
    public async Task Calculate_RangeAndTagIds_PassedToPaymentService()
    {
        GivenPayments();
        int[] tagIds = [1, 2];

        await _service.Calculate(AnyRange, GroupingType.Month, tagIds);

        await _paymentService.Received(1).GetByDateRange(AnyRange, tagIds);
    }

    [Fact]
    public async Task GetPaymentsTable_TagIds_PassedToPaymentService()
    {
        GivenPayments();
        int[] tagIds = [3];

        await _service.GetPaymentsTable(AnyRange, GroupingType.Month, tagIds);

        await _paymentService.Received(1).GetByDateRange(AnyRange, tagIds);
    }

    private void GivenPayments(params Payment[] payments)
    {
        _paymentService.GetByDateRange(Arg.Any<DateRange>(), Arg.Any<int[]?>()).Returns(payments.ToList());
    }

    private Payment Spending(decimal sum, DateTime date) => Spending(sum, date, _food);

    private static Payment Spending(decimal sum, DateTime date, Category? category) => new()
    {
        PaymentSum = sum,
        PaymentDate = date,
        PaymentCategory = category
    };

    // Еда (1) → Кафе (2); Зарплата (3) — доходная.
    private static List<Category> BuildTree()
    {
        var food = new Category(1) { CategoryName = "Еда", OrderIndex = 1 };
        food.Children.Add(new Category(2) { CategoryName = "Кафе", ParentCategoryId = 1, OrderIndex = 2 });
        var salary = new Category(3) { CategoryName = "Зарплата", IsOnlyIncome = true, OrderIndex = 3 };
        return new List<Category> { food, salary };
    }

    private static List<Category> Descendants(List<Category> tree, int categoryId)
    {
        var all = Flatten(tree).ToList();
        var node = all.FirstOrDefault(c => c.CategoryId == categoryId);
        return node is null ? new List<Category>() : Flatten(node.Children).ToList();
    }

    private static IEnumerable<Category> Flatten(IEnumerable<Category> categories)
        => categories.SelectMany(c => new[] { c }.Concat(Flatten(c.Children)));
}
