using FinCtrl.Application.Dashboard;
using FinCtrl.Domain.Dashboard;
using Microsoft.AspNetCore.Mvc;

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

        /// <summary>Траты за сегодня/вчера/неделю/месяц, по категориям и три крупнейшие траты.</summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(DashboardSummaryDto), 200)]
        public async Task<IActionResult> GetSummary()
        {
            return Ok(DashboardSummaryDto.FromModel(await _dashboardService.GetSummary()));
        }

        /// <summary>
        /// График трат с <paramref name="from"/> по <paramref name="to"/> включительно; не заданная дата — по умолчанию
        /// для режима (конец — сегодня). 400 — неизвестный режим или начало позже конца.
        /// </summary>
        [HttpGet("chart")]
        [ProducesResponseType(typeof(ChartDataDto), 200)]
        public async Task<IActionResult> GetChart(
            [FromQuery] ChartMode mode = ChartMode.Day,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var points = await _dashboardService.GetChartData(mode, from?.Date, to?.Date.AddDays(1));

            return Ok(ChartDataDto.FromModel(points));
        }
    }
}
