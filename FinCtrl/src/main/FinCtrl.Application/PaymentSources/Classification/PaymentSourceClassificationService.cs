using FinCtrl.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinCtrl.Application.PaymentSources.Classification;

/// <summary>Автоклассификация источников платежей через LLM.</summary>
public interface IPaymentSourceClassificationService
{
    /// <summary>
    /// Классифицирует источники пачками по <see cref="PaymentSourceClassificationOptions.BatchSize"/> и сохраняет
    /// результат с признаком ИИ-классификации; классификация проставляется и в переданные объекты.
    /// Ошибка классификатора не бросается: уже обработанные пачки остаются сохранёнными, остальные источники — без классификации.
    /// </summary>
    /// <returns><c>null</c> — успешно; иначе текст ошибки для пользователя.</returns>
    Task<string?> Classify(IReadOnlyList<PaymentSource> paymentSources, CancellationToken cancellationToken = default);
}

internal class PaymentSourceClassificationService(
    IPaymentSourceClassifier classifier,
    IPaymentSourceRepository repository,
    IOptions<PaymentSourceClassificationOptions> options,
    ILogger<PaymentSourceClassificationService> logger) : IPaymentSourceClassificationService
{
    private readonly IPaymentSourceClassifier _classifier = classifier;
    private readonly IPaymentSourceRepository _repository = repository;
    private readonly int _batchSize = Math.Max(1, options.Value.BatchSize);
    private readonly ILogger<PaymentSourceClassificationService> _logger = logger;

    public async Task<string?> Classify(IReadOnlyList<PaymentSource> paymentSources, CancellationToken cancellationToken = default)
    {
        foreach (var batch in paymentSources.Chunk(_batchSize))
        {
            IReadOnlyList<PaymentSourceClassification> classifications;
            try
            {
                classifications = await _classifier.Classify(batch, cancellationToken);
            }
            catch (PaymentSourceClassificationException ex)
            {
                _logger.LogWarning(ex, "Не удалось классифицировать {Count} источников платежей", paymentSources.Count);
                return ex.Message;
            }

            var classified = Apply(batch, classifications);
            if (classified.Count > 0)
                await _repository.UpdateClassifications(classified);
        }

        return null;
    }

    // Ответ сопоставляется по id: чужие id и пустые классификации игнорируются.
    private static List<PaymentSource> Apply(PaymentSource[] batch, IReadOnlyList<PaymentSourceClassification> classifications)
    {
        var byId = batch.ToDictionary(x => x.PaymentSourceId);
        var classified = new List<PaymentSource>();

        foreach (var item in classifications)
        {
            if (!byId.Remove(item.PaymentSourceId, out var source) || string.IsNullOrWhiteSpace(item.Classification))
                continue;

            source.SetAiClassification(item.Classification);
            classified.Add(source);
        }

        return classified;
    }
}
