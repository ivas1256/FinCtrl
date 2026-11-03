using FinCtrl.Domain;

namespace FinCtrl.Application.PaymentsImport;

/// <summary>Итог импорта файла.</summary>
/// <param name="NewPayments">Созданные платежи (дубли и дозаполненные не входят).</param>
/// <param name="NewPaymentSources">Созданные источники — с классификацией, если её удалось получить.</param>
/// <param name="ClassificationError">Ошибка автоклассификации новых источников; <c>null</c> — без ошибок.</param>
public record PaymentsImportResult(
    IReadOnlyList<Payment> NewPayments,
    IReadOnlyList<PaymentSource> NewPaymentSources,
    string? ClassificationError);
