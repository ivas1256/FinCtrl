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
            Spending(-7, Today, category: null)          // без категории — не учитывается
        });

        var summary = await _service.GetSummary();

        Assert.Equal(100, summary.TodaySpending);
        Assert.Equal(50, summary.YesterdaySpending);
        Assert.Equal(180, summary.WeekSpending);
        Assert.Equal(200, summary.MonthSpending);
        Assert.Equal(new[] { 100m, 50m, 30m }, summary.TopPayments.Select(p => p.PaymentSum));
    }

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
            Spending(-50, Today, cafe)
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
}
