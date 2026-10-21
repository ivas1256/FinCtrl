using FinCtrl.Domain;

namespace FinCtrl.Website.Controllers.CategoriesApi;

/// <summary>Категория; в дереве (<c>getTree</c>) заполнены <see cref="Children"/>, в плоском списке они пустые.</summary>
public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public bool IsOnlyIncome { get; set; }
    public int OrderIndex { get; set; }
    public string Color { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public List<CategoryDto> Children { get; set; } = new();

    public static CategoryDto FromModel(Category category) => new()
    {
        CategoryId = category.CategoryId,
        CategoryName = category.CategoryName,
        ParentCategoryId = category.ParentCategoryId,
        IsOnlyIncome = category.IsOnlyIncome,
        OrderIndex = category.OrderIndex,
        Color = category.Color,
        CreatedAt = category.CreatedAt,
        LastUpdatedAt = category.LastUpdatedAt,
        Children = category.Children.Select(FromModel).ToList()
    };
}
