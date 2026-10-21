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
}
