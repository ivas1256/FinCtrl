using FinCtrl.Domain.Statistic;

namespace FinCtrl.Website.Controllers.PaymentsApi;

/// <summary>Таблица главной: дерево категорий и периоды (ключ периода → диапазон дат).</summary>
public class GetPaymentsTableResponse
{
    public List<CategoryTreeDto> PaymentsTree { get; set; } = new();
    public Dictionary<string, DateRangeDto> Periods { get; set; } = new();

    /// <summary>Сравнение текущего периода группировки с прошлым.</summary>
    public PaymentsTableComparisonDto? Comparison { get; set; }

    public static GetPaymentsTableResponse FromModel(PaymentsTable table) => new()
    {
        PaymentsTree = table.Tree.Select(CategoryTreeDto.FromModel).ToList(),
        Periods = table.Periods.ToDictionary(x => x.Key, x => DateRangeDto.FromModel(x.Range)),
        Comparison = table.Comparison is null ? null : PaymentsTableComparisonDto.FromModel(table.Comparison)
    };
}
