namespace FinCtrl.Application.PaymentSources.Classification;

/// <summary>Настройки автоклассификации источников (секция <c>AiAssistant</c> в appsettings).</summary>
public class PaymentSourceClassificationOptions
{
    public const string SectionName = "AiAssistant";

    /// <summary>Сколько источников отправлять классификатору одним запросом.</summary>
    public int BatchSize { get; set; } = 30;
}
