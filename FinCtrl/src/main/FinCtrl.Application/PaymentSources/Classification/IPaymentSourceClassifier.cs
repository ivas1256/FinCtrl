using FinCtrl.Domain;

namespace FinCtrl.Application.PaymentSources.Classification;

/// <summary>Классификатор источников платежей (LLM-сервис FinCtrl.AiAssistant).</summary>
public interface IPaymentSourceClassifier
{
    /// <summary>Классифицирует источники одним запросом.</summary>
    /// <returns>Классификации по id источника; источника может не оказаться в ответе.</returns>
    /// <exception cref="PaymentSourceClassificationException">Сервис недоступен, ответил ошибкой или некорректным телом.</exception>
    Task<IReadOnlyList<PaymentSourceClassification>> Classify(IReadOnlyList<PaymentSource> paymentSources, CancellationToken cancellationToken);
}

/// <summary>Классификация одного источника.</summary>
public record PaymentSourceClassification(int PaymentSourceId, string Classification);

/// <summary>Классификатор не смог классифицировать источники; сообщение — для пользователя.</summary>
public class PaymentSourceClassificationException(string message, Exception? innerException = null)
    : Exception(message, innerException);
