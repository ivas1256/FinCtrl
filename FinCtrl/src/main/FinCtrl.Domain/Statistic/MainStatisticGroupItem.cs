using FinCtrl.Common;

namespace FinCtrl.Domain.Statistic
{
    public class MainStatisticGroupItem
    {
        public DateRange Period { get; set; }
        public Category Category { get; set; }
        public decimal TotalSum { get; set; }
        public List<MainStatisticPayment> Payments { get; set; }
    }
}
