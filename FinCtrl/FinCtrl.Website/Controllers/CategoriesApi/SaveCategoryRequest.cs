using System.ComponentModel.DataAnnotations;
using FinCtrl.Domain;

namespace FinCtrl.Website.Controllers.CategoriesApi;

/// <summary>Тело запроса создания и правки категории; id правки передаётся в роуте.</summary>
public class SaveCategoryRequest
{
    [Required(ErrorMessage = "Название категории обязательно")]
    public string CategoryName { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public bool IsOnlyIncome { get; set; }
    public int OrderIndex { get; set; }

    /// <summary>Цвет <c>#RRGGBB</c>; пусто — цвет по умолчанию.</summary>
    public string? Color { get; set; }

    public Category ToModel(int categoryId = 0) => new()
    {
        CategoryId = categoryId,
        CategoryName = CategoryName,
        ParentCategoryId = ParentCategoryId,
        IsOnlyIncome = IsOnlyIncome,
        OrderIndex = OrderIndex,
        Color = Color ?? string.Empty
    };
}
