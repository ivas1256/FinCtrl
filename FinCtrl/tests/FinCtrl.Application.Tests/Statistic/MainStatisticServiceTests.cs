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

    // Фильтр таблицы после «сегодня» (15.10.2025): текущий период сравнения с ним не пересекается и колонку не добавляет.
    private static readonly DateRange TableRange = new(new DateTime(2026, 1, 1), new DateTime(2027, 1, 1));

    private readonly IPaymentService _paymentService = Substitute.For<IPaymentService>();
    private readonly ICategoryService _categoryService = Substitute.For<ICategoryService>();
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly MainStatisticService _service;
    private readonly Category _food = new(1);

    public MainStatisticServiceTests()
    {
        _timeProvider.SetLocalTimeZone(TimeZoneInfo.Utc);
        GivenNow(new DateTime(2025, 10, 15, 14, 0, 0));

        _categoryService.GetCategoriesTree().Returns(_ => BuildTree());
        _categoryService.GetAllChildCategories(Arg.Any<int>(), Arg.Any<List<Category>>())
            .Returns(ci => Descendants(ci.ArgAt<List<Category>>(1), ci.ArgAt<int>(0)));

        _service = new MainStatisticService(_paymentService, _categoryService, _timeProvider);
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

        var table = await _service.GetPaymentsTable(TableRange, GroupingType.Month);

        Assert.Equal(new[] { "012026", "022026" }, table.Periods.Select(p => p.Key));
        Assert.Equal(new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 2, 1)), table.Periods[0].Range);
        Assert.Equal(new[] { 1, 3, CategoryIds.FakeTransfers, CategoryIds.FakeEmpty, CategoryIds.FakeTotal },
            table.Tree.Select(n => n.Category.CategoryId));

        var food = table.Tree[0];
        Assert.Equal(-150, food.Cells["012026"].TotalSum);
        Assert.Equal(2, food.Cells["012026"].Payments.Count);
        Assert.Equal(-30, food.Cells["022026"].TotalSum);
        Assert.Equal(-50, Assert.Single(food.Children).Cells["012026"].TotalSum);

        var salary = table.Tree[1];
        Assert.Equal(new[] { "012026", "022026" }, salary.Cells.Keys);
        Assert.All(salary.Cells.Values, c => Assert.Equal(0, c.TotalSum));

        Assert.All(table.Tree[2].Cells.Values, c => Assert.Equal(0, c.TotalSum));
        Assert.Equal(-7, table.Tree[3].Cells["012026"].TotalSum);
        Assert.Equal(-150, table.Tree[4].Cells["012026"].TotalSum);
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

        var table = await _service.GetPaymentsTable(TableRange, groupingType);

        Assert.Equal(expectedKey, Assert.Single(table.Periods).Key);
    }

    [Fact]
    public async Task GetPaymentsTable_Day_PeriodEndIsNextDay()
    {
        GivenPayments(Spending(-100, new DateTime(2026, 2, 17, 15, 0, 0)));

        var table = await _service.GetPaymentsTable(TableRange, GroupingType.Day);

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

        var table = await _service.GetPaymentsTable(TableRange, GroupingType.Month);

        Assert.Empty(table.Periods);
        Assert.Equal(5, table.Tree.Count); // Еда, Зарплата, «Переводы», «Без категории», «ВСЕГО»
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
            Transfer(-3, new DateTime(2026, 1, 9), category: null),
            Spending(-10, new DateTime(2026, 1, 5), new Category(10) { OrderIndex = 5 }),
            Spending(-20, new DateTime(2026, 1, 5), new Category(11) { OrderIndex = 1 }));

        var result = await _service.Calculate(AnyRange, GroupingType.Month);

        Assert.Equal(new[] { 11, 10, CategoryIds.FakeTransfers, CategoryIds.FakeEmpty, CategoryIds.FakeTotal },
            result.Select(x => x.Category.CategoryId));
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

    // Сегодня 21.10.2026 (среда), если не указано иное; фильтр шире обоих периодов — D = сегодня.
    [Theory]
    [InlineData(GroupingType.Month, "2026-10-21", "102026", "2026-10-01", "2026-11-01", "092026", "2026-09-01")]
    [InlineData(GroupingType.Month, "2026-01-15", "012026", "2026-01-01", "2026-02-01", "122025", "2025-12-01")]
    [InlineData(GroupingType.Week, "2026-10-21", "432026", "2026-10-19", "2026-10-26", "422026", "2026-10-12")]
    [InlineData(GroupingType.Week, "2026-01-01", "532025", "2025-12-29", "2026-01-05", "522025", "2025-12-22")]
    [InlineData(GroupingType.Week, "2026-01-05", "22026", "2026-01-05", "2026-01-12", "532025", "2025-12-29")]
    [InlineData(GroupingType.Quartal, "2026-10-21", "Q42026", "2026-10-01", "2027-01-01", "Q32026", "2026-07-01")]
    [InlineData(GroupingType.Quartal, "2026-02-10", "Q12026", "2026-01-01", "2026-04-01", "Q42025", "2025-10-01")]
    [InlineData(GroupingType.Year, "2026-10-21", "2026", "2026-01-01", "2027-01-01", "2025", "2025-01-01")]
    [InlineData(GroupingType.Day, "2026-10-21", "21.10.2026", "2026-10-21", "2026-10-22", "20.10.2026", "2026-10-20")]
    [InlineData(GroupingType.Day, "2026-01-01", "01.01.2026", "2026-01-01", "2026-01-02", "31.12.2025", "2025-12-31")]
    public async Task GetPaymentsTable_Comparison_CurrentPeriodToDateAndWholePreviousPeriod(
        GroupingType groupingType, string today, string currentKey, string currentFrom, string currentPeriodTo,
        string previousKey, string previousFrom)
    {
        GivenNow(D(today).AddHours(14));
        GivenPayments();

        var table = await _service.GetPaymentsTable(new DateRange(D("2020-01-01"), D("2030-01-01")), groupingType);

        var comparison = table.Comparison!;
        Assert.Equal(D(today), comparison.AsOf);
        Assert.Equal(currentKey, comparison.Current.Key);
        Assert.Equal(new DateRange(D(currentFrom), D(today).AddDays(1)), comparison.Current.Range);
        Assert.Equal(D(currentPeriodTo), comparison.CurrentPeriodTo);
        Assert.Equal(previousKey, comparison.Previous.Key);
        Assert.Equal(new DateRange(D(previousFrom), D(currentFrom)), comparison.Previous.Range);
        Assert.Equal(D(today).AddDays(1) < D(currentPeriodTo), comparison.IsCurrentPartial);
    }

    [Theory]
    [InlineData(GroupingType.Month, "2026-10-31")]
    [InlineData(GroupingType.Week, "2026-10-25")]
    [InlineData(GroupingType.Quartal, "2026-12-31")]
    [InlineData(GroupingType.Year, "2026-12-31")]
    [InlineData(GroupingType.Day, "2026-10-21")]
    public async Task GetPaymentsTable_TodayIsLastDayOfPeriod_CurrentIsNotPartial(GroupingType groupingType, string today)
    {
        GivenNow(D(today).AddHours(23));
        GivenPayments();

        var table = await _service.GetPaymentsTable(new DateRange(D("2020-01-01"), D("2030-01-01")), groupingType);

        Assert.False(table.Comparison!.IsCurrentPartial);
        Assert.Equal(table.Comparison.CurrentPeriodTo, table.Comparison.Current.Range.To);
    }

    [Fact]
    public async Task GetPaymentsTable_FilterEndsInPast_AsOfIsLastFilterDay()
    {
        GivenNow(new DateTime(2026, 10, 21, 14, 0, 0));
        GivenPayments();

        var table = await _service.GetPaymentsTable(DateRange.FromInclusiveDates(D("2026-01-01"), D("2026-03-15")), GroupingType.Month);

        var comparison = table.Comparison!;
        Assert.Equal(D("2026-03-15"), comparison.AsOf);
        Assert.Equal(new DateRange(D("2026-03-01"), D("2026-03-16")), comparison.Current.Range);
        Assert.Equal(new DateRange(D("2026-02-01"), D("2026-03-01")), comparison.Previous.Range);
        Assert.True(comparison.IsCurrentPartial);
    }

    [Fact]
    public async Task GetPaymentsTable_FilterEndsOnLastDayOfPastMonth_CurrentIsWholeMonth()
    {
        GivenNow(new DateTime(2026, 10, 21, 14, 0, 0));
        GivenPayments();

        var table = await _service.GetPaymentsTable(DateRange.FromInclusiveDates(D("2026-01-01"), D("2026-03-31")), GroupingType.Month);

        Assert.Equal(D("2026-03-31"), table.Comparison!.AsOf);
        Assert.Equal(new DateRange(D("2026-03-01"), D("2026-04-01")), table.Comparison.Current.Range);
        Assert.False(table.Comparison.IsCurrentPartial);
    }

    [Fact]
    public async Task GetPaymentsTable_NoRange_AsOfIsToday()
    {
        GivenNow(new DateTime(2026, 10, 21, 14, 0, 0));
        GivenPayments();

        var table = await _service.GetPaymentsTable(null, GroupingType.Month);

        Assert.Equal(D("2026-10-21"), table.Comparison!.AsOf);
        Assert.Equal("102026", table.Comparison.Current.Key);
        Assert.Equal("092026", table.Comparison.Previous.Key);
    }

    [Fact]
    public async Task GetPaymentsTable_Comparison_SumsIncludeDescendantsAndPreviousPeriodOutsideFilter()
    {
        GivenNow(new DateTime(2026, 10, 21, 14, 0, 0));
        var cafe = new Category(2);
        var salary = new Category(3) { IsOnlyIncome = true };
        GivenPayments(
            Spending(-100, D("2026-09-05")),
            Spending(-50, D("2026-09-20"), cafe),
            Spending(500, D("2026-09-10"), salary),
            Spending(-30, D("2026-10-03")),
            Spending(-20, D("2026-10-10"), cafe),
            Spending(-7, D("2026-10-02"), category: null),
            Spending(-999, D("2026-10-25")));  // после D — в Т не входит

        var table = await _service.GetPaymentsTable(DateRange.FromInclusiveDates(D("2026-10-01"), D("2026-10-31")), GroupingType.Month);

        var food = table.Tree[0];
        Assert.Equal((-50m, -150m), (food.Comparison!.Current, food.Comparison.Previous));
        Assert.Equal((-20m, -50m), (food.Children[0].Comparison!.Current, food.Children[0].Comparison!.Previous));
        Assert.Equal((0m, 500m), (table.Tree[1].Comparison!.Current, table.Tree[1].Comparison!.Previous));

        var empty = table.Tree[3];
        Assert.Equal(CategoryIds.FakeEmpty, empty.Category.CategoryId);
        Assert.Equal((-7m, 0m), (empty.Comparison!.Current, empty.Comparison.Previous));

        var total = table.Tree[4];
        Assert.Equal(CategoryIds.FakeTotal, total.Category.CategoryId);
        Assert.Equal((-50m, -150m), (total.Comparison!.Current, total.Comparison.Previous));

        Assert.Equal(new[] { "102026" }, table.Periods.Select(p => p.Key));
        Assert.Equal(-1049, food.Cells["102026"].TotalSum);
    }

    [Fact]
    public async Task GetPaymentsTable_Comparison_SelectedWithTagsFromPreviousStartToAsOf()
    {
        GivenNow(new DateTime(2026, 10, 21, 14, 0, 0));
        GivenPayments();
        int[] tagIds = [3];

        await _service.GetPaymentsTable(DateRange.FromInclusiveDates(D("2026-10-01"), D("2026-10-31")), GroupingType.Quartal, tagIds);

        await _paymentService.Received(1).GetByDateRange(new DateRange(D("2026-07-01"), D("2026-10-22")), tagIds);
    }

    [Fact]
    public async Task GetPaymentsTable_NoPaymentsInComparison_SumsAreZero()
    {
        GivenNow(new DateTime(2026, 10, 21, 14, 0, 0));
        GivenPayments(Spending(-100, D("2026-05-05")));

        var table = await _service.GetPaymentsTable(new DateRange(D("2026-01-01"), D("2027-01-01")), GroupingType.Month);

        Assert.All(table.Tree, n => Assert.Equal((0m, 0m), (n.Comparison!.Current, n.Comparison.Previous)));
    }

    [Fact]
    public async Task GetPaymentsTable_CurrentPeriodWithoutPaymentsInsideFilter_AddedToPeriods()
    {
        GivenNow(new DateTime(2026, 10, 21, 14, 0, 0));
        GivenPayments(Spending(-100, D("2026-09-05")));

        var table = await _service.GetPaymentsTable(DateRange.FromInclusiveDates(D("2026-09-01"), D("2026-10-31")), GroupingType.Month);

        Assert.Equal(new[] { "092026", "102026" }, table.Periods.Select(p => p.Key));
        Assert.Equal(new DateRange(D("2026-10-01"), D("2026-11-01")), table.Periods[1].Range);
        Assert.Equal(0, table.Tree[0].Cells["102026"].TotalSum);
        Assert.Empty(table.Tree[0].Cells["102026"].Payments);
    }

    [Fact]
    public async Task GetPaymentsTable_CurrentPeriodOutsideFilter_NotAddedToPeriods()
    {
        GivenNow(new DateTime(2026, 10, 21, 14, 0, 0));
        GivenPayments(Spending(-100, D("2026-11-05")));

        var table = await _service.GetPaymentsTable(DateRange.FromInclusiveDates(D("2026-11-01"), D("2026-11-30")), GroupingType.Month);

        Assert.Equal(new[] { "112026" }, table.Periods.Select(p => p.Key));
        Assert.Equal("102026", table.Comparison!.Current.Key);
    }

    // --- Переводы между своими счетами ---

    [Fact]
    public async Task Calculate_TransferWithoutCategory_InTransfersRowAndNotInTotal()
    {
        GivenPayments(
            Spending(-100, new DateTime(2026, 1, 5)),
            Transfer(-5000, new DateTime(2026, 1, 6), category: null),
            Transfer(5000, new DateTime(2026, 1, 6), category: null));

        var result = await _service.Calculate(AnyRange, GroupingType.Month);

        var transfers = Assert.Single(result, x => x.Category.CategoryId == CategoryIds.FakeTransfers);
        Assert.Equal("Переводы", transfers.Category.CategoryName);
        Assert.Equal(0, transfers.TotalSum);
        Assert.Equal(2, transfers.Payments.Count);
        Assert.DoesNotContain(result, x => x.Category.CategoryId == CategoryIds.FakeEmpty);
        var total = Assert.Single(result, x => x.Category.CategoryId == CategoryIds.FakeTotal);
        Assert.Equal(-100, total.TotalSum);
        Assert.DoesNotContain(total.Payments, p => p.PaymentType == PaymentTypeEnum.Transfer);
    }

    [Fact]
    public async Task Calculate_TransferWithOldSpendingCategory_InTransfersNotInCategoryNorTotal()
    {
        // Перевод со старой категорией «Еда» (например, осталась от источника) — не трата
        GivenPayments(
            Spending(-100, new DateTime(2026, 1, 5)),
            Transfer(-5000, new DateTime(2026, 1, 6), _food));

        var result = await _service.Calculate(AnyRange, GroupingType.Month);

        Assert.Equal(-100, Assert.Single(result, x => x.Category == _food).TotalSum);
        Assert.Equal(-5000, Assert.Single(result, x => x.Category.CategoryId == CategoryIds.FakeTransfers).TotalSum);
        Assert.Equal(-100, Assert.Single(result, x => x.Category.CategoryId == CategoryIds.FakeTotal).TotalSum);
    }

    [Fact]
    public async Task Calculate_TransferWithIncomeCategory_InTransfersNotInIncome()
    {
        var salary = new Category(3) { IsOnlyIncome = true, OrderIndex = 3 };
        GivenPayments(Transfer(5000, new DateTime(2026, 1, 6), salary));

        var result = await _service.Calculate(AnyRange, GroupingType.Month);

        Assert.DoesNotContain(result, x => x.Category == salary);
        Assert.Equal(5000, Assert.Single(result, x => x.Category.CategoryId == CategoryIds.FakeTransfers).TotalSum);
    }

    [Fact]
    public async Task Calculate_TransferWithSavingsCategory_StaysInSavingsAndNotInTotal()
    {
        var savings = new Category(CategoryIds.Savings) { OrderIndex = 4 };
        GivenPayments(
            Spending(-100, new DateTime(2026, 1, 5)),
            Transfer(-5000, new DateTime(2026, 1, 6), savings));

        var result = await _service.Calculate(AnyRange, GroupingType.Month);

        Assert.Equal(-5000, Assert.Single(result, x => x.Category == savings).TotalSum);
        Assert.DoesNotContain(result, x => x.Category.CategoryId == CategoryIds.FakeTransfers);
        Assert.Equal(-100, Assert.Single(result, x => x.Category.CategoryId == CategoryIds.FakeTotal).TotalSum);
    }

    [Fact]
    public async Task Calculate_TransferWithSavingsSubcategory_StaysInSubcategory()
    {
        var deposit = new Category(40) { ParentCategoryId = CategoryIds.Savings, OrderIndex = 5 };
        _categoryService.GetCategoriesTree().Returns(_ =>
        {
            var tree = BuildTree();
            var savings = new Category(CategoryIds.Savings) { CategoryName = "Накопления", OrderIndex = 4 };
            savings.Children.Add(new Category(40) { CategoryName = "Вклад", ParentCategoryId = CategoryIds.Savings, OrderIndex = 5 });
            tree.Add(savings);
            return tree;
        });
        GivenPayments(Transfer(-5000, new DateTime(2026, 1, 6), deposit));

        var result = await _service.Calculate(AnyRange, GroupingType.Month);

        Assert.Equal(-5000, Assert.Single(result, x => x.Category == deposit).TotalSum);
        Assert.DoesNotContain(result, x => x.Category.CategoryId == CategoryIds.FakeTransfers);
    }

    [Fact]
    public async Task GetPaymentsTable_Transfers_RowBeforeUncategorizedAndOutsideTotalAndComparison()
    {
        GivenPayments(
            Spending(-100, new DateTime(2026, 1, 5)),
            Transfer(-5000, new DateTime(2026, 1, 6), _food));

        var table = await _service.GetPaymentsTable(TableRange, GroupingType.Month);

        var transfers = Assert.Single(table.Tree, n => n.Category.CategoryId == CategoryIds.FakeTransfers);
        Assert.Equal(-5000, transfers.Cells["012026"].TotalSum);
        Assert.Single(transfers.Cells["012026"].Payments);
        Assert.Equal(-100, table.Tree.Single(n => n.Category.CategoryId == CategoryIds.FakeTotal).Cells["012026"].TotalSum);
        Assert.Equal(-100, table.Tree.Single(n => n.Category.CategoryId == 1).Cells["012026"].TotalSum);
        Assert.True(table.Tree.IndexOf(transfers) < table.Tree.FindIndex(n => n.Category.CategoryId == CategoryIds.FakeEmpty));
    }

    [Fact]
    public async Task Calculate_SpendingAndIncomeOnSavingsAccount_InSavingsNotInTotalOrOwnCategory()
    {
        var savings = GivenSavingsInTree();
        var salary = new Category(3) { IsOnlyIncome = true, OrderIndex = 3 };
        GivenPayments(
            Spending(-100, new DateTime(2026, 1, 5)),
            OnSavingsAccount(Spending(-30, new DateTime(2026, 1, 6), _food)),
            OnSavingsAccount(Spending(7, new DateTime(2026, 1, 7), salary)),
            OnSavingsAccount(Transfer(-5000, new DateTime(2026, 1, 8), _food)));

        var result = await _service.Calculate(AnyRange, GroupingType.Month);

        Assert.Equal(-5023, Assert.Single(result, x => x.Category.CategoryId == savings.CategoryId).TotalSum);
        Assert.Equal(-100, Assert.Single(result, x => x.Category.CategoryId == 1).TotalSum);
        Assert.DoesNotContain(result, x => x.Category.CategoryId == 3);
        Assert.DoesNotContain(result, x => x.Category.CategoryId == CategoryIds.FakeTransfers);
        Assert.Equal(-100, Assert.Single(result, x => x.Category.CategoryId == CategoryIds.FakeTotal).TotalSum);
    }

    [Fact]
    public async Task Calculate_TransferPairSavingsAndRegularAccount_InSavingsAndTransfers()
    {
        var savings = GivenSavingsInTree();
        GivenPayments(
            OnSavingsAccount(Transfer(5000, new DateTime(2026, 1, 6), null)),
            Transfer(-5000, new DateTime(2026, 1, 6), null));

        var result = await _service.Calculate(AnyRange, GroupingType.Month);

        Assert.Equal(5000, Assert.Single(result, x => x.Category.CategoryId == savings.CategoryId).TotalSum);
        Assert.Equal(-5000, Assert.Single(result, x => x.Category.CategoryId == CategoryIds.FakeTransfers).TotalSum);
    }

    [Fact]
    public async Task Calculate_SavingsCategoryWithoutAccount_StaysInSavings()
    {
        var savings = GivenSavingsInTree();
        GivenPayments(Spending(-40, new DateTime(2026, 1, 6), new Category(CategoryIds.Savings)));

        var result = await _service.Calculate(AnyRange, GroupingType.Month);

        Assert.Equal(-40, Assert.Single(result, x => x.Category.CategoryId == savings.CategoryId).TotalSum);
        Assert.Equal(0, Assert.Single(result, x => x.Category.CategoryId == CategoryIds.FakeTotal).TotalSum);
    }

    [Fact]
    public async Task GetPaymentsTable_SavingsAccountPayment_InSavingsRowCellAndComparison()
    {
        GivenSavingsInTree();
        GivenNow(new DateTime(2026, 1, 20, 12, 0, 0));
        GivenPayments(
            Spending(-100, new DateTime(2026, 1, 5)),
            OnSavingsAccount(Spending(-30, new DateTime(2026, 1, 6), _food)));

        var table = await _service.GetPaymentsTable(TableRange, GroupingType.Month);

        var savingsNode = Assert.Single(table.Tree, n => n.Category.CategoryId == CategoryIds.Savings);
        Assert.Equal(-30, savingsNode.Cells["012026"].TotalSum);
        Assert.Equal(-30, savingsNode.Comparison.Current);
        Assert.Equal(-100, table.Tree.Single(n => n.Category.CategoryId == 1).Comparison.Current);
        Assert.Equal(-100, table.Tree.Single(n => n.Category.CategoryId == CategoryIds.FakeTotal).Cells["012026"].TotalSum);
    }

    private Category GivenSavingsInTree()
    {
        var savings = new Category(CategoryIds.Savings) { CategoryName = "Накопления", OrderIndex = 4 };
        _categoryService.GetCategoriesTree().Returns(_ =>
        {
            var tree = BuildTree();
            tree.Add(savings);
            return tree;
        });
        return savings;
    }

    private static Payment OnSavingsAccount(Payment payment)
    {
        payment.Account = new Account { AccountId = 9, IsSavings = true };
        return payment;
    }

    private static Payment Transfer(decimal sum, DateTime date, Category? category) => new()
    {
        PaymentType = PaymentTypeEnum.Transfer,
        PaymentSum = sum,
        PaymentDate = date,
        PaymentCategory = category
    };

    private void GivenPayments(params Payment[] payments)
    {
        _paymentService.GetByDateRange(Arg.Any<DateRange>(), Arg.Any<int[]?>())
            .Returns(ci =>
            {
                var range = ci.ArgAt<DateRange>(0);
                return payments.Where(p => p.PaymentDate >= range.From && p.PaymentDate < range.To).ToList();
            });
    }

    private void GivenNow(DateTime localNow)
        => _timeProvider.SetUtcNow(new DateTimeOffset(localNow, TimeSpan.Zero));

    private static DateTime D(string date) => DateTime.Parse(date, System.Globalization.CultureInfo.InvariantCulture);

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
