using FinCtrl.Domain;

namespace FinCtrl.Infrastructure.PaymentSources;

/// <summary>
/// Плоская строка выборки источника платежа вместе с его категорией (LEFT JOIN Categories).
/// </summary>
internal class PaymentSourceRow
{
    public int PaymentSourceId { get; set; }
    public string? PaymentSourceName { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }

    public int? Category_CategoryId { get; set; }
    public string? Category_CategoryName { get; set; }
    public int? Category_ParentCategoryId { get; set; }
    public bool? Category_IsOnlyIncome { get; set; }
    public int? Category_OrderIndex { get; set; }
    public string? Category_Color { get; set; }
    public DateTime? Category_CreatedAt { get; set; }
    public DateTime? Category_LastUpdatedAt { get; set; }

    /// <summary>
    /// Собирает доменный источник платежа с полностью заполненной категорией (или без неё).
    /// </summary>
    public PaymentSource MapToPaymentSource() => new()
    {
        PaymentSourceId = PaymentSourceId,
        PaymentSourceName = PaymentSourceName ?? string.Empty,
        Description = Description,
        CreatedAt = CreatedAt,
        LastUpdatedAt = LastUpdatedAt,
        PaymentSourceCategory = Category_CategoryId is null
            ? null
            : new Category
            {
                CategoryId = Category_CategoryId.Value,
                CategoryName = Category_CategoryName ?? string.Empty,
                ParentCategoryId = Category_ParentCategoryId,
                IsOnlyIncome = Category_IsOnlyIncome ?? false,
                OrderIndex = Category_OrderIndex ?? 0,
                Color = Category_Color ?? Category.DefaultColor,
                CreatedAt = Category_CreatedAt ?? default,
                LastUpdatedAt = Category_LastUpdatedAt,
            },
    };
}
