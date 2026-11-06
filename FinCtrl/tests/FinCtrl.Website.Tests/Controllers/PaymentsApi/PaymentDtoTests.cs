using FinCtrl.Domain;
using FinCtrl.Website.Controllers.PaymentsApi;

namespace FinCtrl.Website.Tests.Controllers.PaymentsApi;

public class PaymentDtoTests
{
    private static readonly DateTime SourceCreatedAt = new(2024, 3, 1, 10, 0, 0);

    [Fact]
    public void FromModel_OwnCategory_EffectiveIsOwnAndSourceIsPayment()
    {
        var payment = new Payment
        {
            PaymentCategory = new Category(7) { CategoryName = "Еда" },
            PaymentSource = new PaymentSource(3, categoryId: 8)
        };

        var dto = PaymentDto.FromModel(payment);

        Assert.Equal(7, dto.Category!.CategoryId);
        Assert.Equal(7, dto.EffectiveCategory!.CategoryId);
        Assert.Equal(PaymentDto.CategorySourcePayment, dto.CategorySource);
    }

    [Fact]
    public void FromModel_OnlySourceCategory_EffectiveIsSourceCategory()
    {
        var payment = new Payment { PaymentSource = new PaymentSource(3, categoryId: 8) };

        var dto = PaymentDto.FromModel(payment);

        Assert.Null(dto.Category);
        Assert.Equal(8, dto.EffectiveCategory!.CategoryId);
        Assert.Equal(PaymentDto.CategorySourceSource, dto.CategorySource);
        Assert.Equal(8, dto.PaymentSource!.CategoryId);
    }

    [Fact]
    public void FromModel_NoCategory_CategoriesAndSourceAreNull()
    {
        var dto = PaymentDto.FromModel(new Payment());

        Assert.Null(dto.Category);
        Assert.Null(dto.EffectiveCategory);
        Assert.Null(dto.CategorySource);
        Assert.Null(dto.PaymentSource);
        Assert.Null(dto.Account);
    }

    [Fact]
    public void FromModel_FullPayment_MapsAllFields()
    {
        var payment = new Payment
        {
            PaymentId = 10,
            PaymentType = PaymentTypeEnum.Spending,
            PaymentSum = -150m,
            PaymentDate = new DateTime(2025, 10, 15),
            PaymentDescription = "Обед",
            ExcludeFromStats = true,
            PaymentSource = new PaymentSource(3, "Кафе у дома", null, "Описание", SourceCreatedAt, null),
            Account = new Account(5, "40817", "Карта", SourceCreatedAt, null) { IsSavings = true },
            TransferId = 4,
            TransferPairPaymentId = 11,
            Tags = { new Tag(1, "Поездка", SourceCreatedAt, null) }
        };

        var dto = PaymentDto.FromModel(payment);

        Assert.Equal(10, dto.PaymentId);
        Assert.Equal(PaymentTypeEnum.Spending, dto.PaymentType);
        Assert.Equal(-150m, dto.PaymentSum);
        Assert.Equal(new DateTime(2025, 10, 15), dto.PaymentDate);
        Assert.Equal("Обед", dto.PaymentDescription);
        Assert.True(dto.ExcludeFromStats);
        Assert.Equal("Кафе у дома", dto.PaymentSource!.PaymentSourceName);
        Assert.Equal("Описание", dto.PaymentSource.Description);
        Assert.Equal(SourceCreatedAt, dto.PaymentSource.CreatedAt);
        Assert.Equal("Карта", dto.Account!.DisplayName);
        Assert.True(dto.Account.IsSavings);
        Assert.Equal((4, 11), (dto.TransferId, dto.TransferPairPaymentId));
        Assert.Equal("Поездка", Assert.Single(dto.Tags).TagName);
    }
}
