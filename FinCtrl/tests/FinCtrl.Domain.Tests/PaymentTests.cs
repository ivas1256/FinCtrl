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

    [Fact]
    public void NormalizeSumSign_SpendingWithPositiveSum_MakesSumNegative()
    {
        var payment = new Payment { PaymentType = PaymentTypeEnum.Spending, PaymentSum = 150m };

        payment.NormalizeSumSign();

        Assert.Equal(-150m, payment.PaymentSum);
    }

    [Fact]
    public void NormalizeSumSign_SpendingWithNegativeSum_KeepsSum()
    {
        var payment = new Payment { PaymentType = PaymentTypeEnum.Spending, PaymentSum = -150m };

        payment.NormalizeSumSign();

        Assert.Equal(-150m, payment.PaymentSum);
    }

    [Theory]
    [InlineData(PaymentTypeEnum.Income, 150)]
    [InlineData(PaymentTypeEnum.Income, -150)]
    [InlineData(PaymentTypeEnum.CardTransfer, 150)]
    public void NormalizeSumSign_NotSpending_KeepsSum(PaymentTypeEnum type, int sum)
    {
        var payment = new Payment { PaymentType = type, PaymentSum = sum };

        payment.NormalizeSumSign();

        Assert.Equal(sum, payment.PaymentSum);
    }
}
