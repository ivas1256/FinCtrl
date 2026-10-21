using FinCtrl.Website.Controllers.CategoriesApi;

namespace FinCtrl.Website.Tests.Controllers.CategoriesApi;

public class SaveCategoryRequestTests
{
    [Fact]
    public void ToModel_NoColor_MapsFieldsWithEmptyColor()
    {
        var request = new SaveCategoryRequest { CategoryName = "Еда", ParentCategoryId = 5, OrderIndex = 2 };

        var category = request.ToModel(7);

        Assert.Equal(7, category.CategoryId);
        Assert.Equal("Еда", category.CategoryName);
        Assert.Equal(5, category.ParentCategoryId);
        Assert.Equal(2, category.OrderIndex);
        Assert.Equal(string.Empty, category.Color);
    }
}
