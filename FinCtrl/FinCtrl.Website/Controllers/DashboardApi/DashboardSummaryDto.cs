using FinCtrl.Domain.Dashboard;

namespace FinCtrl.Website.Controllers.DashboardApi
{
    /// <summary>Сводка дашборда; суммы трат — положительные числа.</summary>
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

        public static DashboardSummaryDto FromModel(DashboardSummary summary) => new()
        {
            TodayDate = summary.TodayDate,
            TodaySpending = summary.TodaySpending,
            YesterdayDate = summary.YesterdayDate,
            YesterdaySpending = summary.YesterdaySpending,
            WeekFrom = summary.WeekFrom,
            WeekTo = summary.WeekTo,
            WeekSpending = summary.WeekSpending,
            MonthFrom = summary.MonthFrom,
            MonthTo = summary.MonthTo,
            MonthSpending = summary.MonthSpending,
            AllCategories = summary.AllCategories.Select(CategorySummaryDto.FromModel).ToList(),
            TopPayments = summary.TopPayments.Select(TopPaymentDto.FromModel).ToList()
        };
    }

    /// <summary>Траты категории и доля от трат месяца, %.</summary>
    public class CategorySummaryDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public string FullCategoryName { get; set; } = string.Empty;
        public decimal TotalSum { get; set; }
        public decimal Percentage { get; set; }

        public static CategorySummaryDto FromModel(CategorySpendingItem item) => new()
        {
            CategoryName = item.CategoryName,
            FullCategoryName = item.FullCategoryName,
            TotalSum = item.TotalSum,
            Percentage = item.Percentage
        };
    }

    /// <summary>Крупная трата; сумма — по модулю.</summary>
    public class TopPaymentDto
    {
        public int PaymentId { get; set; }
        public decimal PaymentSum { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PaymentDescription { get; set; }
        public string? CategoryName { get; set; }
        public string? PaymentSourceName { get; set; }

        public static TopPaymentDto FromModel(TopPayment payment) => new()
        {
            PaymentId = payment.PaymentId,
            PaymentSum = payment.PaymentSum,
            PaymentDate = payment.PaymentDate,
            PaymentDescription = payment.PaymentDescription,
            CategoryName = payment.CategoryName,
            PaymentSourceName = payment.PaymentSourceName
        };
    }
}
