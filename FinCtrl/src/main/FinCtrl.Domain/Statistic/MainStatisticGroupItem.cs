using FinCtrl.Common;

namespace FinCtrl.Domain.Statistic
{
    /// <summary>Сумма и платежи одной категории за один период группировки.</summary>
    public class MainStatisticGroupItem
    {
        public required DateRange Period { get; set; }
        public required Category Category { get; set; }
        public decimal TotalSum { get; set; }
        public List<Payment> Payments { get; set; } = new();
    }
}
