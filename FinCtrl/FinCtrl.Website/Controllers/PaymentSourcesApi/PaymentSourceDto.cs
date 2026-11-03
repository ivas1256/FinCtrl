using FinCtrl.Domain;

namespace FinCtrl.Website.Controllers.PaymentSourcesApi
{
    public class PaymentSourceDto
    {
        public int PaymentSourceId { get; set; }
        public string PaymentSourceName { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string? Description { get; set; }

        [System.ComponentModel.DataAnnotations.MaxLength(200)]
        public string? Classification { get; set; }

        /// <summary>Классификацию задала LLM; на входе (legacy PUT) игнорируется.</summary>
        public bool IsAiClassified { get; set; }

        public DateTime CreatedAt { get; set; }
        public static PaymentSourceDto FromModel(PaymentSource paymentSource) => new PaymentSourceDto
        {
            PaymentSourceId = paymentSource.PaymentSourceId,
            PaymentSourceName = paymentSource.PaymentSourceName,
            CategoryId = paymentSource.PaymentSourceCategory?.CategoryId ?? null,
            Description = paymentSource.Description,
            Classification = paymentSource.Classification,
            IsAiClassified = paymentSource.IsAiClassified,
            CreatedAt = paymentSource.CreatedAt
        };
    }
}