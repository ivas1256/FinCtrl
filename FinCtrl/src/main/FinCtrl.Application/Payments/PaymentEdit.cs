namespace FinCtrl.Application.Payments;

/// <summary>Правка платежа пользователем. <c>null</c> в поле — не трогать его.</summary>
/// <param name="PaymentId">Id платежа.</param>
public record PaymentEdit(int PaymentId)
{
    /// <summary>
    /// Своя категория платежа; <c>null</c> — не трогать. Категория, совпадающая с категорией источника, в платёж
    /// не записывается (итоговая категория и так берётся из источника). Очистить свою категорию нельзя (legacy).
    /// </summary>
    public int? CategoryId { get; init; }

    /// <summary>Счёт; <c>null</c> — не трогать, <c>0</c> — очистить.</summary>
    public int? AccountId { get; init; }

    /// <summary>Описание; <c>null</c> — не трогать, пустая строка (или пробелы) — очистить.</summary>
    public string? Description { get; init; }

    /// <summary>Исключить из статистики; <c>null</c> — не трогать.</summary>
    public bool? ExcludeFromStats { get; init; }

    /// <summary>Теги; <c>null</c> — не трогать, список (в т.ч. пустой) — заменить набор целиком.</summary>
    public IReadOnlyCollection<int>? TagIds { get; init; }

    /// <summary>
    /// Перевод между своими счетами; <c>null</c> — не трогать. <c>true</c> — тип <see cref="Domain.PaymentTypeEnum.Transfer"/>,
    /// <c>false</c> — тип по знаку суммы, связь со второй стороной удаляется (вторая сторона остаётся переводом).
    /// </summary>
    public bool? IsTransfer { get; init; }

    /// <summary>
    /// Вторая сторона перевода; <c>null</c> — не трогать, <c>0</c> — отвязать. Вторая сторона тоже становится переводом.
    /// </summary>
    public int? TransferPairPaymentId { get; init; }
}
