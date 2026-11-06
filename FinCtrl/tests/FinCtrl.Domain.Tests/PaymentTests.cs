using FinCtrl.Domain.PaymentsImport;

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
    [InlineData(PaymentTypeEnum.Transfer, 150)]
    [InlineData(PaymentTypeEnum.Transfer, -150)]
    public void NormalizeSumSign_NotSpending_KeepsSum(PaymentTypeEnum type, int sum)
    {
        var payment = new Payment { PaymentType = type, PaymentSum = sum };

        payment.NormalizeSumSign();

        Assert.Equal(sum, payment.PaymentSum);
    }

    [Theory]
    [InlineData(-150, PaymentTypeEnum.Spending)]
    [InlineData(0, PaymentTypeEnum.Spending)]
    [InlineData(300, PaymentTypeEnum.Income)]
    public void FromExcelPayment_SumSign_DeterminesPaymentType(decimal sum, PaymentTypeEnum expectedType)
    {
        var row = new ExcelPayment(new DateTime(2025, 10, 1), sum, "Магазин", "INV-1", "OK", "");

        var payment = Payment.FromExcelPayment(row, new PaymentSource(1, null), null, new DateTime(2025, 10, 15));

        Assert.Equal(expectedType, payment.PaymentType);
        Assert.Equal(sum, payment.PaymentSum);
    }

    [Fact]
    public void FromExcelPayment_Row_MapsDateSourceAccountAndCreatedAt()
    {
        var createdAt = new DateTime(2025, 10, 15, 14, 30, 0);
        var source = new PaymentSource(1, "Магазин", null, null, createdAt, null);
        var account = new Account(5, "Карта", "Карта", createdAt, null);
        var row = new ExcelPayment(new DateTime(2025, 10, 1, 12, 30, 0), -150m, "Магазин", "INV-1", "OK", "Списание", "Карта");

        var payment = Payment.FromExcelPayment(row, source, account, createdAt);

        Assert.Equal(0, payment.PaymentId);
        Assert.Equal(new DateTime(2025, 10, 1, 12, 30, 0), payment.PaymentDate);
        Assert.Same(source, payment.PaymentSource);
        Assert.Same(account, payment.Account);
        Assert.Equal(createdAt, payment.CreatedAt);
        Assert.Null(payment.PaymentCategory);
        Assert.Null(payment.PaymentDescription);
        Assert.Null(payment.LastUpdatedAt);
        Assert.Empty(payment.Tags);
    }

    [Fact]
    public void Constructor_Ids_CreatesSourceAndCategoryStubs()
    {
        var createdAt = new DateTime(2025, 10, 1);

        var payment = new Payment(10, (int)PaymentTypeEnum.Income, 300m, createdAt, "Зарплата", 2, 7, createdAt, null);

        Assert.Equal(10, payment.PaymentId);
        Assert.Equal(PaymentTypeEnum.Income, payment.PaymentType);
        Assert.Equal(300m, payment.PaymentSum);
        Assert.Equal("Зарплата", payment.PaymentDescription);
        Assert.Equal(2, payment.PaymentSource?.PaymentSourceId);
        Assert.Null(payment.PaymentSource?.PaymentSourceCategory);
        Assert.Equal(7, payment.PaymentCategory?.CategoryId);
    }

    [Fact]
    public void Constructor_NullIds_NoSourceAndCategory()
    {
        var payment = new Payment(10, (int)PaymentTypeEnum.Spending, -1m, new DateTime(2025, 10, 1), "", null, null, new DateTime(2025, 10, 1), null);

        Assert.Null(payment.PaymentSource);
        Assert.Null(payment.PaymentCategory);
        Assert.Null(payment.CalculatedCategory);
    }
}
