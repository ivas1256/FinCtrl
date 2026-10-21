using FinCtrl.Domain.Dashboard;

namespace FinCtrl.Website.Controllers.DashboardApi
{
    /// <summary>Точки графика трат.</summary>
    public class ChartDataDto
    {
        public List<ChartPointDto> Points { get; set; } = new();

        public static ChartDataDto FromModel(IEnumerable<ChartPoint> points) => new()
        {
            Points = points.Select(p => new ChartPointDto { Label = p.Label, Value = p.Value }).ToList()
        };
    }

    /// <summary>Точка графика: подпись периода и сумма трат.</summary>
    public class ChartPointDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }
}
