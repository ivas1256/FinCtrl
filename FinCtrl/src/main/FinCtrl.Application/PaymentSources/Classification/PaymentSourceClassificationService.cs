using FinCtrl.Common.Application;
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

    /// <summary>
    /// Берёт <paramref name="count"/> последних созданных источников без классификации и классифицирует их
    /// так же, как <see cref="Classify"/>.
    /// </summary>
    /// <param name="count">Сколько источников взять, от 1 до <see cref="PaymentSourceClassificationService.MaxUnclassifiedCount"/>.</param>
    /// <exception cref="FinCtrl.Common.Application.ValidationException"><paramref name="count"/> вне допустимого диапазона.</exception>
    Task<UnclassifiedClassificationResult> ClassifyLatestUnclassified(int count, CancellationToken cancellationToken = default);
}

/// <summary>Итог классификации неклассифицированных источников.</summary>
/// <param name="PaymentSources">Отобранные источники; классификация — если её вернула LLM.</param>
/// <param name="Error">Ошибка классификации для пользователя; <c>null</c> — без ошибок.</param>
public record UnclassifiedClassificationResult(IReadOnlyList<PaymentSource> PaymentSources, string? Error);

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

    public const int MaxUnclassifiedCount = 500;

    public async Task<string?> Classify(IReadOnlyList<PaymentSource> paymentSources, CancellationToken cancellationToken = default)
    {
        foreach (var batch in paymentSources.Chunk(_batchSize))
        {
            _logger.LogInformation("Отправка {Count} источников платежей на классификацию в AiAssistant", batch.Length);

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

            _logger.LogInformation(
                "AiAssistant вернул {ResponseCount} классификаций, сохранено {SavedCount} из {Count} источников",
                classifications.Count, classified.Count, batch.Length);
        }

        return null;
    }

    public async Task<UnclassifiedClassificationResult> ClassifyLatestUnclassified(int count, CancellationToken cancellationToken = default)
    {
        if (count < 1 || count > MaxUnclassifiedCount)
            throw new ValidationException($"Количество источников должно быть от 1 до {MaxUnclassifiedCount}");

        _logger.LogInformation("Классификация последних {Count} источников платежей без классификации", count);

        var sources = await _repository.GetLatestUnclassified(count);
        _logger.LogInformation("Найдено {Count} источников платежей без классификации", sources.Count);

        var error = await Classify(sources, cancellationToken);

        if (error is null)
            _logger.LogInformation(
                "Классификация завершена: классифицировано {ClassifiedCount} из {Count} источников",
                sources.Count(x => x.Classification is not null), sources.Count);

        return new UnclassifiedClassificationResult(sources, error);
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
