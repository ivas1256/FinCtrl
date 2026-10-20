using FinCtrl.Common;

namespace FinCtrl.Website.Controllers.PaymentsApi;

/// <summary>Диапазон дат; <see cref="To"/> — исключающая граница.</summary>
public record DateRangeDto(DateTime From, DateTime To)
{
    public static DateRangeDto FromModel(DateRange range) => new(range.From, range.To);
}
