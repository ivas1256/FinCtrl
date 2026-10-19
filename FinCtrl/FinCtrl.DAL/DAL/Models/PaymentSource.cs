using FinCtrl.DAL.Interface;

namespace FinCtrl.DAL.Models

{
    public class PaymentSource : ITimeLoggingEntity
    {
        public PaymentSource() { }
        public PaymentSource(int paymentSourceId, string paymentSourceName, Category? category = null) : this(paymentSourceId, paymentSourceName)
        {            
            Category = category;
        }
        private PaymentSource(int paymentSourceId, string paymentSourceName)
        {
            PaymentSourceId = paymentSourceId;
            PaymentSourceName = paymentSourceName;
        }

        public int PaymentSourceId { get; set; }
        public string PaymentSourceName { get; set; }
        public Category? Category { get; set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime LastUpdatedAt { get; private set; }

        public string? Description { get; set; }

        public int ID => PaymentSourceId;
        //public List<Payment> Payments { get; set; }
    }
}
