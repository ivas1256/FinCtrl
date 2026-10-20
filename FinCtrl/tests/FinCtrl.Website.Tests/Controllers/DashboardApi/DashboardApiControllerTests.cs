using FinCtrl.Application.Dashboard;
using FinCtrl.Domain.Dashboard;
using FinCtrl.Website.Controllers.DashboardApi;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace FinCtrl.Website.Tests.Controllers.DashboardApi;

public class DashboardApiControllerTests
{
    private readonly IDashboardService _dashboardService = Substitute.For<IDashboardService>();
    private readonly DashboardApiController _controller;

    public DashboardApiControllerTests()
    {
        _dashboardService.GetChartData(Arg.Any<ChartMode>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>())
            .Returns(new List<ChartPoint>());
        _controller = new DashboardApiController(_dashboardService);
    }

    [Fact]
    public async Task GetChart_Dates_FromIsDayStartAndToIsNextDay()
    {
        await _controller.GetChart(ChartMode.Week, new DateTime(2025, 10, 1, 15, 0, 0), new DateTime(2025, 10, 31, 9, 0, 0));

        await _dashboardService.Received(1).GetChartData(ChartMode.Week, new DateTime(2025, 10, 1), new DateTime(2025, 11, 1));
    }

    [Fact]
    public async Task GetChart_NoDates_PassesNullsForDefaultRange()
    {
        await _controller.GetChart();

        await _dashboardService.Received(1).GetChartData(ChartMode.Day, null, null);
    }

    [Fact]
    public async Task GetChart_Points_MapsToChartData()
    {
        _dashboardService.GetChartData(Arg.Any<ChartMode>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>())
            .Returns(new List<ChartPoint> { new() { Label = "01.10", Value = 150m } });

        var result = await _controller.GetChart();

        var data = Assert.IsType<ChartDataDto>(Assert.IsType<OkObjectResult>(result).Value);
        var point = Assert.Single(data.Points);
        Assert.Equal(("01.10", 150m), (point.Label, point.Value));
    }

    [Fact]
    public async Task GetSummary_Summary_MapsToDto()
    {
        _dashboardService.GetSummary().Returns(new DashboardSummary { TodaySpending = 100m, TodayDate = new DateTime(2025, 10, 15) });

        var result = await _controller.GetSummary();

        var dto = Assert.IsType<DashboardSummaryDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(100m, dto.TodaySpending);
        Assert.Equal(new DateTime(2025, 10, 15), dto.TodayDate);
    }
}
