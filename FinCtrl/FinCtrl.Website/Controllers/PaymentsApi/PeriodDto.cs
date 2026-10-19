using System.Collections.Generic;

namespace FinCtrl.Website.Controllers.PaymentsApi
{
    public class PeriodDto
    {
        public decimal TotalSum { get; set; }
        public decimal ChildrenSum { get; set; }
        public List<PaymentDto> Payments { get; set; }
    }

}
