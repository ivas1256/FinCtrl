using FinCtrl.Application.Categories;
using FinCtrl.Application.Dashboard;
using FinCtrl.Application.Payments;
using FinCtrl.Common;
using FinCtrl.Domain;
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
            Spending(-1000, Today, new Category(33)),   // накопления — не учитываются
            Spending(500, Today, food),                  // доход — не учитывается
            Spending(-7, Today, category: null)          // без категории — не учитывается
        });

        var summary = await _service.GetSummary();

        Assert.Equal(100, summary.TodaySpending);
        Assert.Equal(50, summary.YesterdaySpending);
        Assert.Equal(180, summary.WeekSpending);
        Assert.Equal(200, summary.MonthSpending);
        Assert.Equal(new[] { -100m, -50m, -30m }, summary.TopPayments.Select(p => p.PaymentSum));
    }

    private static Payment Spending(decimal sum, DateTime date, Category? category) => new()
    {
        PaymentSum = sum,
        PaymentDate = date,
        PaymentCategory = category
    };
}
