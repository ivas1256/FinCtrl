using FinCtrl.Domain.Dashboard;
using FinCtrl.Website.Controllers.DashboardApi;

namespace FinCtrl.Website.Tests.Controllers.DashboardApi;

public class DashboardSummaryDtoTests
{
    [Fact]
    public void FromModel_Summary_MapsPeriodsCategoriesAndTopPayments()
    {
        var summary = new DashboardSummary
        {
            TodayDate = new DateTime(2025, 10, 15),
            TodaySpending = 100m,
            YesterdayDate = new DateTime(2025, 10, 14),
            YesterdaySpending = 50m,
            WeekFrom = new DateTime(2025, 10, 13),
            WeekTo = new DateTime(2025, 10, 15),
            WeekSpending = 180m,
            MonthFrom = new DateTime(2025, 10, 1),
            MonthTo = new DateTime(2025, 10, 15),
            MonthSpending = 200m,
            AllCategories =
            [
                new CategorySpendingItem { CategoryId = 2, CategoryName = "Кафе", FullCategoryName = "Еда / Кафе", TotalSum = 50m, Percentage = 25m }
            ],
            TopPayments =
            [
                new TopPayment
                {
                    PaymentId = 10, PaymentSum = 100m, PaymentDate = new DateTime(2025, 10, 15),
                    PaymentDescription = "Обед", CategoryName = "Кафе", PaymentSourceName = "Кофейня"
                }
            ]
        };

        var dto = DashboardSummaryDto.FromModel(summary);

        Assert.Equal((new DateTime(2025, 10, 15), 100m), (dto.TodayDate, dto.TodaySpending));
        Assert.Equal((new DateTime(2025, 10, 14), 50m), (dto.YesterdayDate, dto.YesterdaySpending));
        Assert.Equal((new DateTime(2025, 10, 13), new DateTime(2025, 10, 15), 180m), (dto.WeekFrom, dto.WeekTo, dto.WeekSpending));
        Assert.Equal((new DateTime(2025, 10, 1), new DateTime(2025, 10, 15), 200m), (dto.MonthFrom, dto.MonthTo, dto.MonthSpending));

        var category = Assert.Single(dto.AllCategories);
        Assert.Equal(("Кафе", "Еда / Кафе", 50m, 25m), (category.CategoryName, category.FullCategoryName, category.TotalSum, category.Percentage));

        var top = Assert.Single(dto.TopPayments);
        Assert.Equal((10, 100m, new DateTime(2025, 10, 15), "Обед", "Кафе", "Кофейня"),
            (top.PaymentId, top.PaymentSum, top.PaymentDate, top.PaymentDescription, top.CategoryName, top.PaymentSourceName));
    }

    [Fact]
    public void FromModel_EmptySummary_EmptyLists()
    {
        var dto = DashboardSummaryDto.FromModel(new DashboardSummary());

        Assert.Empty(dto.AllCategories);
        Assert.Empty(dto.TopPayments);
    }
}
