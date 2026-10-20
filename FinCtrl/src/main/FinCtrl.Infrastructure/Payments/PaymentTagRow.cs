using FinCtrl.Domain;

namespace FinCtrl.Infrastructure.Payments;

/// <summary>
/// Строка выборки тегов платежей: связь <c>PaymentTags</c> вместе с данными тега.
/// </summary>
internal class PaymentTagRow
{
    public int PaymentId { get; set; }
    public int TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }

    /// <summary>Доменный тег из строки.</summary>
    public Tag MapToTag() => new(TagId, TagName, CreatedAt, LastUpdatedAt);
}
