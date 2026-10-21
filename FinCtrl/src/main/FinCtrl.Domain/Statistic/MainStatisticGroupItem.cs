using FinCtrl.Common;

namespace FinCtrl.Domain.Statistic
{
    public class MainStatisticGroupItem
    {
        public required DateRange Period { get; set; }
        public required Category Category { get; set; }
        public decimal TotalSum { get; set; }
        public List<MainStatisticPayment> Payments { get; set; } = new();
    }
}
