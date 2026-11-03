using FinCtrl.Application.PaymentsImport;
using FinCtrl.Website.Controllers.PaymentSourcesApi;

namespace FinCtrl.Website.Controllers.PaymentsApi;

/// <summary>Итог импорта файла.</summary>
public class UploadPaymentsResponse
{
    /// <summary>Созданные платежи (дубли не входят).</summary>
    public List<PaymentDto> NewPayments { get; set; } = new();

    /// <summary>Созданные источники; классификация — если её вернула LLM.</summary>
    public List<PaymentSourceDto> NewPaymentSources { get; set; } = new();

    /// <summary>Ошибка автоклассификации новых источников; <c>null</c> — без ошибок.</summary>
    public string? ClassificationError { get; set; }

    public static UploadPaymentsResponse FromModel(PaymentsImportResult result) => new()
    {
        NewPayments = result.NewPayments.Select(PaymentDto.FromModel).ToList(),
        NewPaymentSources = result.NewPaymentSources.Select(PaymentSourceDto.FromModel).ToList(),
        ClassificationError = result.ClassificationError
    };
}
