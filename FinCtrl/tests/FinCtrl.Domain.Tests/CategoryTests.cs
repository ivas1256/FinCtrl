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
        var category = new Category(33);

        Assert.False(category.IsIncludeInTotalResults());
    }

    [Fact]
    public void IsIncludeInTotalResults_FakeEmptyCategory_ReturnsFalse()
    {
        Assert.False(Category.CreateFakeEmpty().IsIncludeInTotalResults());
    }
}
