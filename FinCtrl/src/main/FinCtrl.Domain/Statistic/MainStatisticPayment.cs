namespace FinCtrl.Domain.Statistic
{
    public class MainStatisticPayment
    {
        public int PaymentId { get; set; }
        public PaymentTypeEnum PaymentType { get; set; }
        public decimal PaymentSum { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentSource? PaymentSource { get; set; }
        public Account? Account { get; set; }
        public Category? Category { get; set; }
        public string? PaymentDescription { get; set; }
        public bool ExcludeFromStats { get; set; }
        public List<Tag> Tags { get; set; } = new();

        public static MainStatisticPayment FromPayment(Payment payment) => new MainStatisticPayment()
        {
            PaymentId = payment.PaymentId,
            PaymentType = payment.PaymentType,
            PaymentSum = payment.PaymentSum,
            PaymentDate = payment.PaymentDate,
            PaymentSource = payment.PaymentSource,
            Account = payment.Account,
            Category = payment.CalculatedCategory,
            PaymentDescription = payment.PaymentDescription,
            ExcludeFromStats = payment.ExcludeFromStats,
            Tags = payment.Tags
        };
    }
}
