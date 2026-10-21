namespace FinCtrl.Domain.Tests;

public class CategoryTests
{
    [Fact]
    public void IsIncludeInTotalResults_RegularSpendingCategory_ReturnsTrue()
    {
        var category = new Category(1);

        Assert.True(category.IsIncludeInTotalResults());
    }

    [Fact]
    public void IsIncludeInTotalResults_OnlyIncomeCategory_ReturnsFalse()
    {
        var category = new Category(1) { IsOnlyIncome = true };

        Assert.False(category.IsIncludeInTotalResults());
    }

    [Fact]
    public void IsIncludeInTotalResults_SavingsCategory_ReturnsFalse()
    {
        var category = new Category(CategoryIds.Savings);

        Assert.False(category.IsIncludeInTotalResults());
    }

    [Fact]
    public void IsIncludeInTotalResults_FakeEmptyCategory_ReturnsFalse()
    {
        Assert.False(Category.CreateFakeEmpty().IsIncludeInTotalResults());
    }

    [Fact]
    public void IsFake_FakeCategories_ReturnsTrue()
    {
        Assert.True(Category.CreateFakeEmpty().IsFake);
        Assert.True(Category.CreateFakeTotal().IsFake);
        Assert.False(new Category(1).IsFake);
    }

    [Theory]
    [InlineData("", Category.DefaultColor)]
    [InlineData("  ", Category.DefaultColor)]
    [InlineData("#112233", "#112233")]
    public void ColorOrDefault_EmptyColor_ReturnsDefault(string color, string expected)
    {
        Assert.Equal(expected, new Category(1) { Color = color }.ColorOrDefault);
    }

    [Fact]
    public void Equals_SameIdDifferentFields_ReturnsTrueAndSameHashCode()
    {
        var first = new Category(7) { CategoryName = "Еда", OrderIndex = 1 };
        var second = new Category(7) { CategoryName = "Продукты", OrderIndex = 5 };

        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentIdNullOrOtherType_ReturnsFalse()
    {
        var category = new Category(7);

        Assert.False(category.Equals(new Category(8)));
        Assert.False(category.Equals(null));
        Assert.False(category.Equals(7));
    }

    [Fact]
    public void Equals_UsedInCollections_ComparesById()
    {
        var categories = new List<Category> { new(1), new(2) };

        Assert.Contains(new Category(2), categories);
        Assert.Single(new[] { new Category(3), new Category(3) }.Distinct());
    }

    [Fact]
    public void CreateFakeEmpty_NameAndOrderBeforeTotal()
    {
        var empty = Category.CreateFakeEmpty();
        var total = Category.CreateFakeTotal();

        Assert.Equal(CategoryIds.FakeEmpty, empty.CategoryId);
        Assert.Equal("Без категории", empty.CategoryName);
        Assert.Equal(CategoryIds.FakeTotal, total.CategoryId);
        Assert.Equal("ВСЕГО потрачено:", total.CategoryName);
        Assert.True(empty.OrderIndex < total.OrderIndex);
        Assert.Empty(empty.Children);
    }

    [Fact]
    public void Updated_SetsLastUpdatedAtToPassedTime()
    {
        var now = new DateTime(2025, 10, 15, 14, 30, 0);
        var category = new Category(7);

        category.Updated(now);

        Assert.Equal(now, category.LastUpdatedAt);
    }
}
