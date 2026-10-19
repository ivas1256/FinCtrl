using FinCtrl.Application.Dashboard;
using FinCtrl.Common;
using FinCtrl.Domain.Dashboard;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FinCtrl.Website.Controllers.DashboardApi
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardApiController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardApiController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("summary")]
        [ProducesResponseType(typeof(DashboardSummaryDto), 200)]
        public async Task<IActionResult> GetSummary()
        {
            var s = await _dashboardService.GetSummary();
            var total = s.MonthSpending > 0 ? s.MonthSpending : 1;

            return Ok(new DashboardSummaryDto
            {
                TodayDate = s.TodayDate,
                TodaySpending = s.TodaySpending,
                YesterdayDate = s.YesterdayDate,
                YesterdaySpending = s.YesterdaySpending,
                WeekFrom = s.WeekFrom,
                WeekTo = s.WeekTo,
                WeekSpending = s.WeekSpending,
                MonthFrom = s.MonthFrom,
                MonthTo = s.MonthTo,
                MonthSpending = s.MonthSpending,
                AllCategories = s.AllCategories
                    .Select(c => new CategorySummaryDto
                    {
                        CategoryName = c.CategoryName,
                        FullCategoryName = c.FullCategoryName,
                        TotalSum = c.TotalSum,
                        Percentage = Math.Round(c.TotalSum / total * 100, 1)
                    })
                    .ToList(),
                TopPayments = s.TopPayments
                    .Select(p => new TopPaymentDto
                    {
                        PaymentId = p.PaymentId,
                        PaymentSum = Math.Abs(p.PaymentSum),
                        PaymentDate = p.PaymentDate,
                        PaymentDescription = p.PaymentDescription,
                        CategoryName = p.CalculatedCategory?.CategoryName,
                        PaymentSourceName = p.PaymentSource?.PaymentSourceName
                    })
                    .ToList()
            });
        }

        [HttpGet("chart")]
        [ProducesResponseType(typeof(ChartDataDto), 200)]
        public async Task<IActionResult> GetChart(
            [FromQuery] ChartMode mode = ChartMode.Day,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var today = DateTime.Today;

            var range = mode switch
            {
                ChartMode.Day   => new DateRange(from?.Date ?? today.AddDays(-29), to?.Date ?? today),
                ChartMode.Week  => new DateRange(from?.Date ?? GetWeekStart(today).AddDays(-21), to?.Date ?? today),
                ChartMode.Month => new DateRange(from?.Date ?? new DateTime(today.Year, 1, 1), to?.Date ?? today),
                _ => new DateRange(today.AddDays(-29), today)
            };

            var points = await _dashboardService.GetChartData(mode, range);

            return Ok(new ChartDataDto
            {
                Points = points.Select(p => new ChartPointDto { Label = p.Label, Value = p.Value }).ToList()
            });
        }

        private static DateTime GetWeekStart(DateTime date)
            => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)).Date;
    }
}
