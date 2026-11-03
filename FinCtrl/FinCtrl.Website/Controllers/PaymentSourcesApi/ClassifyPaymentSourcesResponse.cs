using FinCtrl.Application.PaymentSources.Classification;

namespace FinCtrl.Website.Controllers.PaymentSourcesApi;

/// <summary>Итог классификации неклассифицированных источников.</summary>
public class ClassifyPaymentSourcesResponse
{
    /// <summary>Источники, отправленные на классификацию; классификация — если её вернула LLM.</summary>
    public List<PaymentSourceDto> PaymentSources { get; set; } = new();

    /// <summary>Ошибка классификации; <c>null</c> — без ошибок.</summary>
    public string? ClassificationError { get; set; }

    public static ClassifyPaymentSourcesResponse FromModel(UnclassifiedClassificationResult result) => new()
    {
        PaymentSources = result.PaymentSources.Select(PaymentSourceDto.FromModel).ToList(),
        ClassificationError = result.Error
    };
}
