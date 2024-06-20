using FinCtrl.DAL.Interface;

namespace FinCtrl.DAL.Models
{
    public class Payment : ITimeLoggingEntity
    {
        public Payment() { }

        public Payment(DateTime date, decimal sum)
        {
            PaymentDate = date;
            PaymentSum = sum;
        }

        public Payment(int paymentId, PaymentTypes paymentType, decimal paymentSum, DateTime paymentDate, PaymentSource paymentSource)
            : this(paymentId, paymentType, paymentSum, paymentDate)
        {
            PaymentSource = paymentSource;
        }
        private Payment(int paymentId, PaymentTypes paymentType, decimal paymentSum, DateTime paymentDate)
        {
            PaymentId = paymentId;
            PaymentType = paymentType;
            PaymentSum = paymentSum;
            PaymentDate = paymentDate;
            CreatedAt = DateTime.Now;
        }

        public int PaymentId { get; set; }
        public PaymentTypes PaymentType { get; set; }
        public decimal PaymentSum { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentSource PaymentSource { get; set; }
        public Category? PaymentCategory { get; set; }
        public string? PaymentDescription { get; set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime LastUpdatedAt { get; private set; }

        public int ID => PaymentId;

        public Category SelectedCategory
        {
            get
            {
                return PaymentCategory ?? PaymentSource?.Category;
            }
        }
    }
}
