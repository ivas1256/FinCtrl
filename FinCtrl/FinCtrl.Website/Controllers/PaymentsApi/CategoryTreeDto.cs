using FinCtrl.Domain.Statistic;

namespace FinCtrl.Website.Controllers.PaymentsApi;

/// <summary>Строка таблицы главной: категория, её ячейки по ключам периодов и дочерние строки.</summary>
public class CategoryTreeDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public bool IsOnlyIncome { get; set; }
    public string Color { get; set; } = string.Empty;
    public Dictionary<string, PeriodDto> Periods { get; set; } = new();
    public List<CategoryTreeDto> Children { get; set; } = new();

    public static CategoryTreeDto FromModel(PaymentsTableNode node) => new()
    {
        CategoryId = node.Category.CategoryId,
        CategoryName = node.Category.CategoryName,
        ParentCategoryId = node.Category.ParentCategoryId,
        IsOnlyIncome = node.Category.IsOnlyIncome,
        Color = node.Category.ColorOrDefault,
        Periods = node.Cells.ToDictionary(x => x.Key, x => PeriodDto.FromModel(x.Value)),
        Children = node.Children.Select(FromModel).ToList()
    };
}
