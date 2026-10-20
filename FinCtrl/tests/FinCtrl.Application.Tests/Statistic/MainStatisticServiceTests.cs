using FinCtrl.Application.Payments;
using FinCtrl.Application.Statistic;
using FinCtrl.Common;
using FinCtrl.Domain;
using FinCtrl.Domain.Statistic;
using NSubstitute;

namespace FinCtrl.Application.Tests.Statistic;

public class MainStatisticServiceTests
{
    private static readonly DateRange AnyRange = new(new DateTime(2025, 1, 1), new DateTime(2027, 1, 1));

    private readonly IPaymentService _paymentService = Substitute.For<IPaymentService>();
    private readonly MainStatisticService _service;
    private readonly Category _food = new(1);

    public MainStatisticServiceTests()
    {
        _service = new MainStatisticService(_paymentService);
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

    private void GivenPayments(params Payment[] payments)
    {
        _paymentService.GetByDateRange(Arg.Any<DateRange>(), Arg.Any<int[]?>()).Returns(payments.ToList());
    }

    private Payment Spending(decimal sum, DateTime date) => new()
    {
        PaymentSum = sum,
        PaymentDate = date,
        PaymentCategory = _food
    };
}
