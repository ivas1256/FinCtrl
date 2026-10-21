using FinCtrl.Application.Payments;

namespace FinCtrl.Website.Controllers.PaymentsApi;

/// <summary>
/// Тело запроса правки платежа; id платежа — из роута (<c>paymentId</c> в теле, если передан, игнорируется).
/// Поле со значением <c>null</c> (или отсутствующее) не меняется.
/// </summary>
public class EditPaymentRequest
{
    /// <summary>Своя категория; <c>null</c> — не трогать (очистить нельзя), категория источника в платёж не записывается.</summary>
    public int? CategoryId { get; set; }

    /// <summary>Счёт; <c>null</c> — не трогать, <c>0</c> — очистить.</summary>
    public int? AccountId { get; set; }

    /// <summary>Описание; <c>null</c> — не трогать, пустая строка — очистить.</summary>
    public string? Description { get; set; }

    /// <summary>Исключить из статистики; <c>null</c> — не трогать.</summary>
    public bool? ExcludeFromStats { get; set; }

    /// <summary>Теги; <c>null</c> — не трогать, пустой или непустой список — заменить набор целиком.</summary>
    public List<int>? TagIds { get; set; }

    public PaymentEdit ToModel(int paymentId) => new(paymentId)
    {
        CategoryId = CategoryId,
        AccountId = AccountId,
        Description = Description,
        ExcludeFromStats = ExcludeFromStats,
        TagIds = TagIds
    };
}
