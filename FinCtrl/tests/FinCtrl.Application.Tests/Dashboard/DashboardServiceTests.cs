using System.Globalization;
using FinCtrl.Application.Categories;
using FinCtrl.Application.Dashboard;
using FinCtrl.Application.Payments;
using FinCtrl.Common;
using FinCtrl.Common.Application;
using FinCtrl.Domain;
using FinCtrl.Domain.Dashboard;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FinCtrl.Application.Tests.Dashboard;

public class DashboardServiceTests
{
    // Среда, 15.10.2025: неделя начинается в понедельник 13.10, месяц — 01.10
    private static readonly DateTime Today = new(2025, 10, 15);

    private readonly IPaymentService _paymentService = Substitute.For<IPaymentService>();
    private readonly ICategoryService _categoryService = Substitute.For<ICategoryService>();
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetLocalTimeZone(TimeZoneInfo.Utc);
        timeProvider.SetUtcNow(new DateTimeOffset(Today.AddHours(14), TimeSpan.Zero));

        _categoryService.GetCategoriesTree().Returns(new List<Category>());
        _service = new DashboardService(_paymentService, _categoryService, timeProvider);
    }

    [Fact]
    public async Task GetSummary_FixedTime_CalculatesPeriodsFromProvidedTime()
    {
        _paymentService.GetByDateRange(Arg.Any<DateRange>()).Returns(new List<Payment>());

        var summary = await _service.GetSummary();

        Assert.Equal(Today, summary.TodayDate);
        Assert.Equal(Today.AddDays(-1), summary.YesterdayDate);
        Assert.Equal(new DateTime(2025, 10, 13), summary.WeekFrom);
        Assert.Equal(Today, summary.WeekTo);
        Assert.Equal(new DateTime(2025, 10, 1), summary.MonthFrom);
        Assert.Equal(Today, summary.MonthTo);
        await _paymentService.Received(1).GetByDateRange(
            Arg.Is<DateRange>(r => r.From == new DateTime(2025, 10, 1) && r.To == Today.AddDays(1)));
    }

    [Fact]
    public async Task GetSummary_SpendingsInDifferentPeriods_SumsOnlyIncludedSpendings()
    {
        var food = new Category(1);
        _paymentService.GetByDateRange(Arg.Any<DateRange>()).Returns(new List<Payment>
        {
            Spending(-100, Today, food),
            Spending(-50, Today.AddDays(-1), food),
            Spending(-30, new DateTime(2025, 10, 13), food),
            Spending(-20, new DateTime(2025, 10, 2), food),
            Spending(-1000, Today, new Category(CategoryIds.Savings)),   // накопления — не учитываются
            Spending(500, Today, food),                  // доход — не учитывается
            Spending(-7, Today, category: null),         // без категории — не учитывается
            Transfer(-5000, Today, food),                // перевод между своими счетами — не трата
            new Payment { PaymentSum = -900, PaymentDate = Today, PaymentCategory = food, Account = new Account { IsSavings = true } } // накопительный счёт
        });

        var summary = await _service.GetSummary();

        Assert.Equal(100, summary.TodaySpending);
        Assert.Equal(50, summary.YesterdaySpending);
        Assert.Equal(180, summary.WeekSpending);
        Assert.Equal(200, summary.MonthSpending);
        Assert.Equal(new[] { 100m, 50m, 30m }, summary.TopPayments.Select(p => p.PaymentSum));
    }

    private static Payment Transfer(decimal sum, DateTime date, Category? category) => new()
    {
        PaymentType = PaymentTypeEnum.Transfer,
        PaymentSum = sum,
        PaymentDate = date,
        PaymentCategory = category
    };

    private static Payment Spending(decimal sum, DateTime date, Category? category) => new()
    {
        PaymentSum = sum,
        PaymentDate = date,
        PaymentCategory = category
    };

    [Fact]
    public async Task GetSummary_Categories_PercentageOfMonthSpending()
    {
        var food = new Category(1) { CategoryName = "Еда" };
        var cafe = new Category(2) { CategoryName = "Кафе", ParentCategoryId = 1 };
        food.Children.Add(cafe);
        _categoryService.GetCategoriesTree().Returns(new List<Category> { food, Category.CreateFakeEmpty() });
        _paymentService.GetByDateRange(Arg.Any<DateRange>()).Returns(new List<Payment>
        {
            Spending(-150, Today, food),
            Spending(-50, Today, cafe),
            Transfer(-5000, Today, cafe)
        });

        var summary = await _service.GetSummary();

        Assert.Equal(
            new[] { ("Еда", 150m, 75m), ("Еда / Кафе", 50m, 25m) },
            summary.AllCategories.Select(c => (c.FullCategoryName, c.TotalSum, c.Percentage)));
    }

    [Fact]
    public async Task GetSummary_NoSpendings_ZeroPercentage()
    {
        _categoryService.GetCategoriesTree().Returns(new List<Category> { new(1) { CategoryName = "Еда" } });
        _paymentService.GetByDateRange(Arg.Any<DateRange>()).Returns(new List<Payment>());

        var summary = await _service.GetSummary();

        Assert.Equal(0, Assert.Single(summary.AllCategories).Percentage);
    }

    [Theory]
    [InlineData(ChartMode.Day, "2025-09-16", 30)]
    [InlineData(ChartMode.Week, "2025-09-22", 4)]
    [InlineData(ChartMode.Month, "2025-01-01", 10)]
    public async Task GetChartData_NoRange_UsesDefaultRangeForMode(ChartMode mode, string expectedFrom, int expectedPoints)
    {
        _paymentService.GetByDateRange(Arg.Any<DateRange>()).Returns(new List<Payment>());

        var points = await _service.GetChartData(mode);

        Assert.Equal(expectedPoints, points.Count);
        await _paymentService.Received(1).GetByDateRange(new DateRange(DateTime.Parse(expectedFrom), Today.AddDays(1)));
    }

    [Fact]
    public async Task GetChartData_Day_ToIsExclusive()
    {
        var food = new Category(1);
        _paymentService.GetByDateRange(Arg.Any<DateRange>()).Returns(new List<Payment>
        {
            Spending(-100, new DateTime(2025, 10, 2), food)
        });

        var points = await _service.GetChartData(ChartMode.Day, new DateTime(2025, 10, 1), new DateTime(2025, 10, 4));

        Assert.Equal(new[] { ("01.10", 0m), ("02.10", 100m), ("03.10", 0m) }, points.Select(p => (p.Label, p.Value)));
    }

    [Fact]
    public async Task GetChartData_Week_LastLabelEndsOnLastDay()
    {
        _paymentService.GetByDateRange(Arg.Any<DateRange>()).Returns(new List<Payment>());

        var points = await _service.GetChartData(ChartMode.Week, new DateTime(2025, 10, 6), Today.AddDays(1));

        Assert.Equal(new[] { "06.10–12.10", "13.10–15.10" }, points.Select(p => p.Label));
    }

    [Fact]
    public async Task GetChartData_UnknownModeOrReversedRange_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.GetChartData((ChartMode)42));
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.GetChartData(ChartMode.Day, Today, Today));
    }

    [Fact]
    public async Task GetChartData_Month_MonthsFromFirstDayWithEmptyMonths()
    {
        var food = new Category(1);
        _paymentService.GetByDateRange(Arg.Any<DateRange>()).Returns(new List<Payment>
        {
            Spending(-10, new DateTime(2025, 8, 31, 23, 0, 0), food),
            Spending(-30, new DateTime(2025, 10, 1), food),
            Spending(-5, Today, food)
        });

        var points = await _service.GetChartData(ChartMode.Month, new DateTime(2025, 8, 15), Today.AddDays(1));

        var ru = new CultureInfo("ru-RU");
        Assert.Equal(
            new[]
            {
                (new DateTime(2025, 8, 1).ToString("MMM yy", ru), 10m),
                (new DateTime(2025, 9, 1).ToString("MMM yy", ru), 0m),
                (new DateTime(2025, 10, 1).ToString("MMM yy", ru), 35m)
            },
            points.Select(p => (p.Label, p.Value)));
    }

    [Theory]
    [InlineData(ChartMode.Day)]
    [InlineData(ChartMode.Week)]
    [InlineData(ChartMode.Month)]
    public async Task GetChartData_AnyMode_CountsOnlyIncludedSpendings(ChartMode mode)
    {
        _paymentService.GetByDateRange(Arg.Any<DateRange>()).Returns(new List<Payment>
        {
            Spending(-100, Today, new Category(1)),
            Spending(500, Today, new Category(1)),                              // доход
            Spending(-1000, Today, new Category(CategoryIds.Savings)),          // накопления
            Spending(-50, Today, new Category(3) { IsOnlyIncome = true }),      // доходная категория
            Spending(-7, Today, category: null),                                // без категории
            Transfer(-5000, Today, new Category(1))                              // перевод между своими счетами
        });

        var points = await _service.GetChartData(mode, Today, Today.AddDays(1));

        Assert.Equal(100m, Assert.Single(points).Value);
    }

    [Fact]
    public async Task GetChartData_Week_PaymentsOnWeekBoundariesIncluded()
    {
        var food = new Category(1);
        _paymentService.GetByDateRange(Arg.Any<DateRange>()).Returns(new List<Payment>
        {
            Spending(-1, new DateTime(2025, 10, 6), food),               // понедельник
            Spending(-2, new DateTime(2025, 10, 12, 23, 0, 0), food),    // воскресенье
            Spending(-4, new DateTime(2025, 10, 13), food)               // понедельник следующей недели
        });

        var points = await _service.GetChartData(ChartMode.Week, new DateTime(2025, 10, 8), Today.AddDays(1));

        Assert.Equal(new[] { ("06.10–12.10", 3m), ("13.10–15.10", 4m) }, points.Select(p => (p.Label, p.Value)));
    }

    [Fact]
    public async Task GetChartData_TimeInBounds_DroppedBeforeQuery()
    {
        _paymentService.GetByDateRange(Arg.Any<DateRange>()).Returns(new List<Payment>());

        var points = await _service.GetChartData(ChartMode.Day, new DateTime(2025, 10, 1, 15, 0, 0), new DateTime(2025, 10, 3, 9, 0, 0));

        Assert.Equal(new[] { "01.10", "02.10" }, points.Select(p => p.Label));
        await _paymentService.Received(1).GetByDateRange(new DateRange(new DateTime(2025, 10, 1), new DateTime(2025, 10, 3)));
    }

    [Fact]
    public async Task GetSummary_FirstDayOfMonth_YesterdayAndWeekFromPreviousMonthNotInMonth()
    {
        // Среда, 01.10.2025: неделя с понедельника 29.09, вчера — 30.09
        var service = CreateServiceAt(new DateTime(2025, 10, 1));
        var food = new Category(1);
        _paymentService.GetByDateRange(Arg.Any<DateRange>()).Returns(new List<Payment>
        {
            Spending(-10, new DateTime(2025, 9, 29), food),
            Spending(-20, new DateTime(2025, 9, 30), food),
            Spending(-40, new DateTime(2025, 10, 1), food)
        });

        var summary = await service.GetSummary();

        await _paymentService.Received(1).GetByDateRange(new DateRange(new DateTime(2025, 9, 29), new DateTime(2025, 10, 2)));
        Assert.Equal(new DateTime(2025, 9, 29), summary.WeekFrom);
        Assert.Equal(new DateTime(2025, 10, 1), summary.MonthFrom);
        Assert.Equal(40, summary.TodaySpending);
        Assert.Equal(20, summary.YesterdaySpending);
        Assert.Equal(70, summary.WeekSpending);
        Assert.Equal(40, summary.MonthSpending);
    }

    [Fact]
    public async Task GetSummary_MondayFirstDayOfMonth_FetchesFromYesterday()
    {
        // Понедельник, 01.12.2025: неделя и месяц начинаются сегодня, вчера — раньше обоих
        var service = CreateServiceAt(new DateTime(2025, 12, 1));
        _paymentService.GetByDateRange(Arg.Any<DateRange>()).Returns(new List<Payment>());

        var summary = await service.GetSummary();

        await _paymentService.Received(1).GetByDateRange(new DateRange(new DateTime(2025, 11, 30), new DateTime(2025, 12, 2)));
        Assert.Equal(new DateTime(2025, 12, 1), summary.WeekFrom);
        Assert.Equal(new DateTime(2025, 12, 1), summary.WeekTo);
    }

    [Fact]
    public async Task GetSummary_Sunday_WeekFromMondayToToday()
    {
        var sunday = new DateTime(2025, 10, 19);
        var service = CreateServiceAt(sunday);
        _paymentService.GetByDateRange(Arg.Any<DateRange>()).Returns(new List<Payment>());

        var summary = await service.GetSummary();

        Assert.Equal(new DateTime(2025, 10, 13), summary.WeekFrom);
        Assert.Equal(sunday, summary.WeekTo);
    }

    [Fact]
    public async Task GetSummary_TopPayments_ThreeLargestWithFields()
    {
        var cafe = new Category(2) { CategoryName = "Кафе" };
        var source = new PaymentSource(5, "Кофейня", null, null, Today, null) { PaymentSourceCategory = cafe };
        _paymentService.GetByDateRange(Arg.Any<DateRange>()).Returns(new List<Payment>
        {
            new() { PaymentId = 1, PaymentSum = -10, PaymentDate = Today, PaymentCategory = cafe },
            new() { PaymentId = 2, PaymentSum = -300, PaymentDate = Today, PaymentSource = source, PaymentDescription = "Капучино" },
            new() { PaymentId = 3, PaymentSum = -200, PaymentDate = Today.AddDays(-1), PaymentCategory = cafe },
            new() { PaymentId = 4, PaymentSum = -100, PaymentDate = Today, PaymentCategory = cafe }
        });

        var summary = await _service.GetSummary();

        Assert.Equal(new[] { 2, 3, 4 }, summary.TopPayments.Select(p => p.PaymentId));
        var top = summary.TopPayments[0];
        Assert.Equal(300, top.PaymentSum);
        Assert.Equal(Today, top.PaymentDate);
        Assert.Equal("Капучино", top.PaymentDescription);
        Assert.Equal("Кафе", top.CategoryName);
        Assert.Equal("Кофейня", top.PaymentSourceName);
        Assert.Null(summary.TopPayments[1].PaymentSourceName);
    }

    [Fact]
    public async Task GetSummary_Categories_SourceCategoryCountedAndSortedBySumThenName()
    {
        var food = new Category(3) { CategoryName = "Еда" };
        _categoryService.GetCategoriesTree().Returns(new List<Category>
        {
            new(1) { CategoryName = "Быт" },
            new(2) { CategoryName = "Авто" },
            food
        });
        var source = new PaymentSource(5, "Магазин", null, null, Today, null) { PaymentSourceCategory = food };
        _paymentService.GetByDateRange(Arg.Any<DateRange>()).Returns(new List<Payment>
        {
            new() { PaymentSum = -100, PaymentDate = Today, PaymentSource = source }
        });

        var summary = await _service.GetSummary();

        Assert.Equal(
            new[] { ("Еда", 100m, 100m), ("Авто", 0m, 0m), ("Быт", 0m, 0m) },
            summary.AllCategories.Select(c => (c.FullCategoryName, c.TotalSum, c.Percentage)));
        Assert.Equal(3, summary.AllCategories[0].CategoryId);
    }

    private DashboardService CreateServiceAt(DateTime today)
    {
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetLocalTimeZone(TimeZoneInfo.Utc);
        timeProvider.SetUtcNow(new DateTimeOffset(today.AddHours(14), TimeSpan.Zero));
        return new DashboardService(_paymentService, _categoryService, timeProvider);
    }
}
