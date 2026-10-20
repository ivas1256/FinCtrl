namespace FinCtrl.Website.Controllers.DashboardApi
{
    public class DashboardSummaryDto
    {
        public decimal TodaySpending { get; set; }
        public DateTime TodayDate { get; set; }
        public decimal YesterdaySpending { get; set; }
        public DateTime YesterdayDate { get; set; }
        public decimal WeekSpending { get; set; }
        public DateTime WeekFrom { get; set; }
        public DateTime WeekTo { get; set; }
        public decimal MonthSpending { get; set; }
        public DateTime MonthFrom { get; set; }
        public DateTime MonthTo { get; set; }
        public List<CategorySummaryDto> AllCategories { get; set; } = new();
        public List<TopPaymentDto> TopPayments { get; set; } = new();
    }

    public class CategorySummaryDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public string FullCategoryName { get; set; } = string.Empty;
        public decimal TotalSum { get; set; }
        public decimal Percentage { get; set; }
    }

    public class TopPaymentDto
    {
        public int PaymentId { get; set; }
        public decimal PaymentSum { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PaymentDescription { get; set; }
        public string? CategoryName { get; set; }
        public string? PaymentSourceName { get; set; }
    }

    public class ChartDataDto
    {
        public List<ChartPointDto> Points { get; set; } = new();
    }

    public class ChartPointDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }
}