using System.ComponentModel.DataAnnotations;

namespace FinCtrl.Website.Controllers.PaymentSourcesApi;

/// <summary>
/// Тело запроса правки одного источника платежа; id источника передаётся в роуте.
/// </summary>
public class EditPaymentSourceRequest
{
    [Required]
    public string PaymentSourceName { get; set; } = string.Empty;

    /// <summary>Категория источника; <c>null</c> — снять категорию.</summary>
    public int? CategoryId { get; set; }

    public string? Description { get; set; }

    /// <summary>Классификация; <c>null</c>/пустая строка — очистить.</summary>
    [MaxLength(200)]
    public string? Classification { get; set; }
}
