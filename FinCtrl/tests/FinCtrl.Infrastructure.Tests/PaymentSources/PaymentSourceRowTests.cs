using FinCtrl.Domain;
using FinCtrl.Infrastructure.PaymentSources;

namespace FinCtrl.Infrastructure.Tests.PaymentSources;

public class PaymentSourceRowTests
{
    private static readonly DateTime Created = new(2025, 10, 1, 12, 0, 0);
    private static readonly DateTime Updated = new(2025, 10, 2, 12, 0, 0);

    [Fact]
    public void MapToPaymentSource_WithCategory_MapsSourceAndFullCategory()
    {
        var row = new PaymentSourceRow
        {
            PaymentSourceId = 3,
            PaymentSourceName = "Магазин",
            Description = "у дома",
            CreatedAt = Created,
            LastUpdatedAt = Updated,
            Classification = "Супермаркеты",
            IsAiClassified = true,
            Category_CategoryId = 7,
            Category_CategoryName = "Еда",
            Category_ParentCategoryId = 1,
            Category_IsOnlyIncome = true,
            Category_OrderIndex = 2,
            Category_Color = "#00ff00",
            Category_CreatedAt = Created,
            Category_LastUpdatedAt = Updated
        };

        var source = row.MapToPaymentSource();

        Assert.Equal((3, "Магазин", "у дома", Created, (DateTime?)Updated),
            (source.PaymentSourceId, source.PaymentSourceName, source.Description, source.CreatedAt, source.LastUpdatedAt));
        Assert.Equal(("Супермаркеты", true), (source.Classification, source.IsAiClassified));
        var category = Assert.IsType<Category>(source.PaymentSourceCategory);
        Assert.Equal((7, "Еда", (int?)1, true, 2, "#00ff00", Created, (DateTime?)Updated),
            (category.CategoryId, category.CategoryName, category.ParentCategoryId, category.IsOnlyIncome, category.OrderIndex,
             category.Color, category.CreatedAt, category.LastUpdatedAt));
    }

    [Fact]
    public void MapToPaymentSource_CategoryWithNullFields_UsesDefaults()
    {
        var row = new PaymentSourceRow { PaymentSourceId = 3, Category_CategoryId = 7 };

        var category = row.MapToPaymentSource().PaymentSourceCategory!;

        Assert.Equal(string.Empty, category.CategoryName);
        Assert.False(category.IsOnlyIncome);
        Assert.Equal(0, category.OrderIndex);
        Assert.Equal(Category.DefaultColor, category.Color);
        Assert.Equal(default, category.CreatedAt);
    }

    [Fact]
    public void MapToPaymentSource_WithoutCategory_CategoryNullAndNameEmpty()
    {
        var row = new PaymentSourceRow { PaymentSourceId = 3, PaymentSourceName = null };

        var source = row.MapToPaymentSource();

        Assert.Null(source.PaymentSourceCategory);
        Assert.Equal(string.Empty, source.PaymentSourceName);
    }
}
