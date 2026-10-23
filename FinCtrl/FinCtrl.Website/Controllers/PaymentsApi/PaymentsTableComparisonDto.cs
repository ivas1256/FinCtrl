using FinCtrl.Domain.Statistic;

namespace FinCtrl.Website.Controllers.PaymentsApi;

/// <summary>
/// Периоды сравнения таблицы главной: D (<see cref="AsOf"/>), текущий период по D включительно и прошлый целиком.
/// Суммы строк — в <see cref="CategoryTreeDto.Comparison"/>.
/// </summary>
public class PaymentsTableComparisonDto
{
    public DateTime AsOf { get; set; }
    public ComparisonPeriodDto Current { get; set; } = null!;
    public ComparisonPeriodDto Previous { get; set; } = null!;

    /// <summary>Конец полного текущего периода (исключающая граница).</summary>
    public DateTime CurrentPeriodTo { get; set; }

    public bool IsCurrentPartial { get; set; }

    public static PaymentsTableComparisonDto FromModel(PaymentsTableComparison comparison) => new()
    {
        AsOf = comparison.AsOf,
        Current = ComparisonPeriodDto.FromModel(comparison.Current),
        Previous = ComparisonPeriodDto.FromModel(comparison.Previous),
        CurrentPeriodTo = comparison.CurrentPeriodTo,
        IsCurrentPartial = comparison.IsCurrentPartial
    };
}

/// <summary>Период сравнения: ключ периода группировки и диапазон дат; <see cref="To"/> — исключающая граница.</summary>
public record ComparisonPeriodDto(string Key, DateTime From, DateTime To)
{
    public static ComparisonPeriodDto FromModel(PaymentsTablePeriod period) => new(period.Key, period.Range.From, period.Range.To);
}

/// <summary>Суммы строки (вместе с потомками) за текущий и прошлый периоды сравнения.</summary>
public record NodeComparisonDto(decimal Current, decimal Previous)
{
    public static NodeComparisonDto FromModel(PaymentsTableNodeComparison comparison) => new(comparison.Current, comparison.Previous);
}
