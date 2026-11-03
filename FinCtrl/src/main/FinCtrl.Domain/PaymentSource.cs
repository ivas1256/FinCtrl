namespace FinCtrl.Domain
{
    public class PaymentSource
    {
        public PaymentSource()
        {
            
        }
        public PaymentSource(int paymentSourceId, int? categoryId)
        {
            PaymentSourceId = paymentSourceId;
            PaymentSourceCategory = categoryId is not null ? new Category(categoryId.Value) : null;
        }

        public PaymentSource(int paymentSourceId, string paymentSourceName, int? paymentSourceCategoryId, string? description, DateTime createdAt, DateTime? lastUpdatedAt)
        {
            PaymentSourceId = paymentSourceId;
            PaymentSourceName = paymentSourceName;
            PaymentSourceCategory = paymentSourceCategoryId is not null ? new Category(paymentSourceCategoryId.Value): null;
            Description = description;
            CreatedAt = createdAt;
            LastUpdatedAt = lastUpdatedAt;
        }

        public int PaymentSourceId { get; set; }
        public string PaymentSourceName { get; set; } = string.Empty;
        public Category? PaymentSourceCategory { get; set; }
        public string? Description { get;  set; }
        public DateTime CreatedAt { get;  set; }
        public DateTime? LastUpdatedAt { get;  set; }

        /// <summary>Человекочитаемая классификация контрагента (группа, по которой сводятся источники).</summary>
        public string? Classification { get; set; }

        /// <summary>Классификацию задала LLM (FinCtrl.AiAssistant), а не человек.</summary>
        public bool IsAiClassified { get; set; }

        public void Updated(DateTime now)
        {
            LastUpdatedAt = now;
        }

        /// <summary>
        /// Классификация, заданная пользователем: пробелы по краям обрезаются, пустая строка — очистить.
        /// Признак <see cref="IsAiClassified"/> сбрасывается, только если значение действительно изменилось.
        /// </summary>
        public void SetClassificationByUser(string? classification)
        {
            var normalized = NormalizeClassification(classification);
            if (normalized == Classification)
                return;

            Classification = normalized;
            IsAiClassified = false;
        }

        /// <summary>Классификация, полученная от LLM.</summary>
        public void SetAiClassification(string classification)
        {
            Classification = NormalizeClassification(classification);
            IsAiClassified = Classification is not null;
        }

        private static string? NormalizeClassification(string? classification) =>
            string.IsNullOrWhiteSpace(classification) ? null : classification.Trim();

        public static PaymentSource CreateNew(string name, DateTime createdAt) => new PaymentSource(0, null)
        {
            PaymentSourceName = name,
            CreatedAt = createdAt,
        };
    }
}
