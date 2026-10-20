namespace FinCtrl.Domain.Tests;

public class PaymentTests
{
    [Fact]
    public void CalculatedCategory_OwnCategorySet_ReturnsOwnCategory()
    {
        var own = new Category(1);
        var payment = new Payment
        {
            PaymentCategory = own,
            PaymentSource = new PaymentSource(10, categoryId: 2)
        };

        Assert.Same(own, payment.CalculatedCategory);
    }

    [Fact]
    public void CalculatedCategory_NoOwnCategory_ReturnsSourceCategory()
    {
        var source = new PaymentSource(10, categoryId: 2);
        var payment = new Payment { PaymentSource = source };

        Assert.Same(source.PaymentSourceCategory, payment.CalculatedCategory);
    }

    [Fact]
    public void CalculatedCategory_NoCategories_ReturnsNull()
    {
        var payment = new Payment { PaymentSource = new PaymentSource(10, categoryId: null) };

        Assert.Null(payment.CalculatedCategory);
    }

    [Fact]
    public void Updated_SetsLastUpdatedAtToPassedTime()
    {
        var now = new DateTime(2026, 10, 20, 12, 30, 0);
        var payment = new Payment();

        payment.Updated(now);

        Assert.Equal(now, payment.LastUpdatedAt);
    }
}
