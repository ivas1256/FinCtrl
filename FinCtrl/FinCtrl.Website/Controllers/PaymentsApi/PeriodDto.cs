namespace FinCtrl.Website.Controllers.PaymentsApi
{
    public class PeriodDto
    {
        public decimal TotalSum { get; set; }
        public List<PaymentDto> Payments { get; set; } = new();
    }

}