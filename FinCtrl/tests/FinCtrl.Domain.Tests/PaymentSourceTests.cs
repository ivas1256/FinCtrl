namespace FinCtrl.Domain.Tests;

public class PaymentSourceTests
{
    private static readonly DateTime Now = new(2025, 10, 15, 14, 30, 0);

    [Fact]
    public void CreateNew_Name_NewSourceWithoutCategory()
    {
        var source = PaymentSource.CreateNew("Магазин у дома", Now);

        Assert.Equal(0, source.PaymentSourceId);
        Assert.Equal("Магазин у дома", source.PaymentSourceName);
        Assert.Null(source.PaymentSourceCategory);
        Assert.Null(source.Description);
        Assert.Equal(Now, source.CreatedAt);
    }

    [Fact]
    public void CreateFromExisting_WithCategory_KeepsPassedCategoryInstance()
    {
        var food = new Category(7) { CategoryName = "Еда" };

        var source = PaymentSource.CreateFromExisting(42, "Магазин", food, "у дома");

        Assert.Equal(42, source.PaymentSourceId);
        Assert.Equal("Магазин", source.PaymentSourceName);
        Assert.Same(food, source.PaymentSourceCategory);
        Assert.Equal("у дома", source.Description);
    }

    [Fact]
    public void CreateFromExisting_WithoutCategory_CategoryIsNull()
    {
        var source = PaymentSource.CreateFromExisting(42, "Магазин", null, null);

        Assert.Null(source.PaymentSourceCategory);
        Assert.Null(source.Description);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(null)]
    public void Constructor_CategoryId_CreatesCategoryStubOnlyForId(int? categoryId)
    {
        var source = new PaymentSource(42, categoryId);

        Assert.Equal(42, source.PaymentSourceId);
        Assert.Equal(categoryId, source.PaymentSourceCategory?.CategoryId);
    }

    [Fact]
    public void Updated_SetsLastUpdatedAtToPassedTime()
    {
        var source = new PaymentSource(42, "Магазин", null, null, Now.AddDays(-1), null);

        source.Updated(Now);

        Assert.Equal(Now, source.LastUpdatedAt);
    }
}
