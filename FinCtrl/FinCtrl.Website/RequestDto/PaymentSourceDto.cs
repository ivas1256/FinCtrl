using FinCtrl.Domain;

namespace FinCtrl.Website.RequestDto
{
    public class PaymentSourceDto
    {
        public int PaymentSourceId { get; set; }
        public string PaymentSourceName { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }
        public static PaymentSourceDto FromModel(PaymentSource paymentSource) => new PaymentSourceDto
        {
            PaymentSourceId = paymentSource.PaymentSourceId,
            PaymentSourceName = paymentSource.PaymentSourceName,
            CategoryId = paymentSource.PaymentSourceCategory?.CategoryId ?? null,
            Description = paymentSource.Description,
            CreatedAt = paymentSource.CreatedAt
        };
    }
}