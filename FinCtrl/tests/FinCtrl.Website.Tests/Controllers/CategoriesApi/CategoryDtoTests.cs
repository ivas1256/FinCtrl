using FinCtrl.Domain;
using FinCtrl.Website.Controllers.CategoriesApi;

namespace FinCtrl.Website.Tests.Controllers.CategoriesApi;

public class CategoryDtoTests
{
    [Fact]
    public void FromModel_Tree_MapsFieldsAndChildrenRecursively()
    {
        var root = new Category(1) { CategoryName = "Еда", OrderIndex = 3, Color = "#112233", IsOnlyIncome = false };
        var child = new Category(2) { CategoryName = "Кафе", ParentCategoryId = 1 };
        child.Children.Add(new Category(3) { CategoryName = "Кофе", ParentCategoryId = 2 });
        root.Children.Add(child);

        var dto = CategoryDto.FromModel(root);

        Assert.Equal(1, dto.CategoryId);
        Assert.Equal("Еда", dto.CategoryName);
        Assert.Equal(3, dto.OrderIndex);
        Assert.Equal("#112233", dto.Color);
        var childDto = Assert.Single(dto.Children);
        Assert.Equal(1, childDto.ParentCategoryId);
        Assert.Equal("Кофе", Assert.Single(childDto.Children).CategoryName);
    }
}
