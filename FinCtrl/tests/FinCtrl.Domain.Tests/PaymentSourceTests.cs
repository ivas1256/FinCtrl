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
    public void SetClassificationByUser_ChangedValue_SetsTrimmedValueAndResetsAiFlag()
    {
        var source = new PaymentSource { Classification = "Продукты", IsAiClassified = true };

        source.SetClassificationByUser("  Кафе  ");

        Assert.Equal("Кафе", source.Classification);
        Assert.False(source.IsAiClassified);
    }

    [Theory]
    [InlineData("Продукты")]
    [InlineData(" Продукты ")]
    public void SetClassificationByUser_SameValue_KeepsAiFlag(string classification)
    {
        var source = new PaymentSource { Classification = "Продукты", IsAiClassified = true };

        source.SetClassificationByUser(classification);

        Assert.Equal("Продукты", source.Classification);
        Assert.True(source.IsAiClassified);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SetClassificationByUser_Empty_ClearsClassificationAndAiFlag(string? classification)
    {
        var source = new PaymentSource { Classification = "Продукты", IsAiClassified = true };

        source.SetClassificationByUser(classification);

        Assert.Null(source.Classification);
        Assert.False(source.IsAiClassified);
    }

    [Fact]
    public void SetAiClassification_Value_SetsTrimmedValueAndAiFlag()
    {
        var source = new PaymentSource { Classification = "Старое" };

        source.SetAiClassification(" Продукты ");

        Assert.Equal("Продукты", source.Classification);
        Assert.True(source.IsAiClassified);
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
