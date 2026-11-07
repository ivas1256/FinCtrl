using FinCtrl.Domain;
using FinCtrl.Infrastructure.Payments;

namespace FinCtrl.Infrastructure.Tests.Payments;

public class PaymentDtoTests
{
    private static readonly DateTime Created = new(2025, 10, 1, 12, 0, 0);
    private static readonly DateTime Updated = new(2025, 10, 2, 12, 0, 0);

    [Fact]
    public void MapToPayment_FullRow_MapsPaymentSourceAccountAndCategories()
    {
        var row = new PaymentDto
        {
            PaymentId = 10,
            PaymentType = (int)PaymentTypeEnum.Spending,
            PaymentSum = -150m,
            PaymentDate = new DateTime(2025, 10, 1),
            PaymentDescription = "Продукты",
            PaymentCreatedAt = Created,
            PaymentLastUpdatedAt = Updated,
            ExcludeFromStats = true,
            TransferId = 4,
            TransferPairPaymentId = 11,
            PaymentSourceId = 3,
            PaymentSource_PaymentSourceId = 3,
            PaymentSource_PaymentSourceName = "Магазин",
            PaymentSource_Description = "у дома",
            PaymentSource_CreatedAt = Created,
            PaymentSource_LastUpdatedAt = Updated,
            PaymentSource_Classification = "Супермаркеты",
            PaymentSource_IsAiClassified = true,
            PaymentSourceCategory_CategoryId = 8,
            PaymentSourceCategory_CategoryName = "Продукты",
            PaymentSourceCategory_ParentCategoryId = 1,
            PaymentSourceCategory_IsOnlyIncome = false,
            PaymentSourceCategory_OrderIndex = 4,
            PaymentSourceCategory_CreatedAt = Created,
            PaymentSourceCategory_LastUpdatedAt = Updated,
            AccountId = 5,
            Account_AccountId = 5,
            Account_ExcelAccountName = "Карта *1234",
            Account_DisplayName = "Основная",
            Account_IsSavings = true,
            Account_CreatedAt = Created,
            Account_LastUpdatedAt = Updated,
            PaymentCategoryCategoryId = 7,
            Category_CategoryId = 7,
            Category_CategoryName = "Еда",
            Category_ParentCategoryId = 1,
            Category_IsOnlyIncome = false,
            Category_OrderIndex = 2,
            Category_CreatedAt = Created,
            Category_LastUpdatedAt = Updated
        };

        var payment = row.MapToPayment();

        Assert.Equal(10, payment.PaymentId);
        Assert.Equal(PaymentTypeEnum.Spending, payment.PaymentType);
        Assert.Equal(-150m, payment.PaymentSum);
        Assert.Equal(new DateTime(2025, 10, 1), payment.PaymentDate);
        Assert.Equal("Продукты", payment.PaymentDescription);
        Assert.Equal(Created, payment.CreatedAt);
        Assert.Equal(Updated, payment.LastUpdatedAt);
        Assert.True(payment.ExcludeFromStats);
        Assert.Equal((4, 11), (payment.TransferId, payment.TransferPairPaymentId));

        var source = Assert.IsType<PaymentSource>(payment.PaymentSource);
        Assert.Equal((3, "Магазин", "у дома", Created, (DateTime?)Updated),
            (source.PaymentSourceId, source.PaymentSourceName, source.Description, source.CreatedAt, source.LastUpdatedAt));
        Assert.Equal(("Супермаркеты", true), (source.Classification, source.IsAiClassified));
        var sourceCategory = Assert.IsType<Category>(source.PaymentSourceCategory);
        Assert.Equal((8, "Продукты", (int?)1, false, 4, Created, (DateTime?)Updated),
            (sourceCategory.CategoryId, sourceCategory.CategoryName, sourceCategory.ParentCategoryId, sourceCategory.IsOnlyIncome,
             sourceCategory.OrderIndex, sourceCategory.CreatedAt, sourceCategory.LastUpdatedAt));

        var account = Assert.IsType<Account>(payment.Account);
        Assert.Equal((5, "Карта *1234", "Основная", Created, (DateTime?)Updated),
            (account.AccountId, account.ExcelAccountName, account.DisplayName, account.CreatedAt, account.LastUpdatedAt));
            Assert.True(account.IsSavings);

        var category = Assert.IsType<Category>(payment.PaymentCategory);
        Assert.Equal((7, "Еда", (int?)1, false, 2, Created, (DateTime?)Updated),
            (category.CategoryId, category.CategoryName, category.ParentCategoryId, category.IsOnlyIncome,
             category.OrderIndex, category.CreatedAt, category.LastUpdatedAt));
        Assert.Same(category, payment.CalculatedCategory);
        Assert.Empty(payment.Tags);
    }

    [Fact]
    public void MapToPayment_NoReferences_SourceAccountAndCategoryAreNull()
    {
        var row = new PaymentDto { PaymentId = 10, PaymentType = (int)PaymentTypeEnum.Income, PaymentSum = 300m };

        var payment = row.MapToPayment();

        Assert.Null(payment.PaymentSource);
        Assert.Null(payment.Account);
        Assert.Null(payment.TransferId);
        Assert.Null(payment.TransferPairPaymentId);
        Assert.Null(payment.PaymentCategory);
        Assert.Null(payment.CalculatedCategory);
    }

    [Fact]
    public void MapToPayment_TransferPairAccount_MappedWhenIdPresent()
    {
        var row = new PaymentDto
        {
            TransferPairAccount_AccountId = 9,
            TransferPairAccount_ExcelAccountName = "Копилка *9",
            TransferPairAccount_DisplayName = "Накопления",
            TransferPairAccount_IsSavings = true,
            TransferPairAccount_CreatedAt = Created,
            TransferPairAccount_LastUpdatedAt = Updated
        };

        var account = Assert.IsType<Account>(row.MapToPayment().TransferPairAccount);

        Assert.Equal((9, "Копилка *9", "Накопления", true, Created, (DateTime?)Updated),
            (account.AccountId, account.ExcelAccountName, account.DisplayName, account.IsSavings, account.CreatedAt, account.LastUpdatedAt));
    }

    [Fact]
    public void MapToPayment_NoTransferPairAccountId_TransferPairAccountIsNull()
    {
        var row = new PaymentDto { TransferPairAccount_DisplayName = "лишнее" };

        Assert.Null(row.MapToPayment().TransferPairAccount);
    }

    [Fact]
    public void MapToPayment_ReferencesWithoutNames_SourceCategoryNullAndNamesEmpty()
    {
        var row = new PaymentDto
        {
            PaymentSourceId = 3,
            PaymentSource_PaymentSourceId = 3,
            AccountId = 5,
            Account_AccountId = 5,
            PaymentCategoryCategoryId = 7,
            Category_CategoryId = 7
        };

        var payment = row.MapToPayment();

        Assert.Null(payment.PaymentSource!.PaymentSourceCategory);
        Assert.Equal(string.Empty, payment.PaymentSource.PaymentSourceName);
        Assert.Equal(string.Empty, payment.Account!.ExcelAccountName);
        Assert.Equal(string.Empty, payment.Account.DisplayName);
        Assert.Equal(string.Empty, payment.PaymentCategory!.CategoryName);
    }

    [Fact]
    public void MapToPayment_SourceCategoryWithNullFields_UsesDefaults()
    {
        var row = new PaymentDto
        {
            PaymentSourceId = 3,
            PaymentSource_PaymentSourceId = 3,
            PaymentSourceCategory_CategoryId = 8
        };

        var category = row.MapToPayment().PaymentSource!.PaymentSourceCategory!;

        Assert.Equal(8, category.CategoryId);
        Assert.Equal(string.Empty, category.CategoryName);
        Assert.False(category.IsOnlyIncome);
        Assert.Equal(0, category.OrderIndex);
        Assert.Equal(default, category.CreatedAt);
    }

    [Theory]
    [InlineData(PaymentTypeEnum.Spending, 150, -150)]
    [InlineData(PaymentTypeEnum.Spending, -150, -150)]
    [InlineData(PaymentTypeEnum.Income, 150, 150)]
    public void MapToPayment_SumSign_NormalizedForSpending(PaymentTypeEnum type, decimal storedSum, decimal expectedSum)
    {
        var row = new PaymentDto { PaymentType = (int)type, PaymentSum = storedSum };

        Assert.Equal(expectedSum, row.MapToPayment().PaymentSum);
    }
}
