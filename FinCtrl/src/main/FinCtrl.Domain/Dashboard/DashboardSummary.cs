namespace FinCtrl.Domain.Dashboard
{
    /// <summary>Read-модель сводки дашборда; суммы трат — положительные числа.</summary>
    public class DashboardSummary
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
        public List<CategorySpendingItem> AllCategories { get; set; } = new();
        public List<TopPayment> TopPayments { get; set; } = new();
    }

    public class CategorySpendingItem
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string FullCategoryName { get; set; } = string.Empty;
        public decimal TotalSum { get; set; }

        /// <summary>Доля от трат текущего месяца, %, с точностью 0,1.</summary>
        public decimal Percentage { get; set; }
    }

    /// <summary>Крупная трата; <see cref="PaymentSum"/> — по модулю.</summary>
    public class TopPayment
    {
        public int PaymentId { get; set; }
        public decimal PaymentSum { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PaymentDescription { get; set; }
        public string? CategoryName { get; set; }
        public string? PaymentSourceName { get; set; }
    }

    public class ChartPoint
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public enum ChartMode { Day, Week, Month }
}
