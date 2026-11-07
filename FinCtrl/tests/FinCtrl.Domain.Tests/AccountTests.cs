namespace FinCtrl.Domain.Tests;

public class AccountTests
{
    private static readonly DateTime Now = new(2025, 10, 15, 14, 30, 0);

    [Fact]
    public void CreateNew_ExcelName_DisplayNameEqualsExcelNameAndCreatedAtSet()
    {
        var account = Account.CreateNew("Карта *1234", Now);

        Assert.Equal(0, account.AccountId);
        Assert.Equal("Карта *1234", account.ExcelAccountName);
        Assert.Equal("Карта *1234", account.DisplayName);
        Assert.Equal(Now, account.CreatedAt);
        Assert.Null(account.LastUpdatedAt);
    }

    [Fact]
    public void CreateFromExisting_Fields_SetsIdAndNamesWithoutDates()
    {
        var account = Account.CreateFromExisting(5, "Карта *1234", "Основная карта", true);

        Assert.Equal(5, account.AccountId);
        Assert.Equal("Карта *1234", account.ExcelAccountName);
        Assert.Equal("Основная карта", account.DisplayName);
        Assert.True(account.IsSavings);
        Assert.Equal(default, account.CreatedAt);
        Assert.Null(account.LastUpdatedAt);
    }

    [Fact]
    public void CreateNew_AccountNumber_TrimsNumber()
    {
        var account = Account.CreateNew("Карта", Now, " 40817810 ");

        Assert.Equal("40817810", account.AccountNumber);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateFromExisting_EmptyAccountNumber_NumberIsNull(string? accountNumber)
    {
        var account = Account.CreateFromExisting(5, "Карта", "Карта", false, accountNumber);

        Assert.Null(account.AccountNumber);
    }

    [Fact]
    public void Updated_SetsLastUpdatedAtToPassedTime()
    {
        var account = new Account(5, "Карта", "Карта", Now.AddDays(-1), null);

        account.Updated(Now);

        Assert.Equal(Now, account.LastUpdatedAt);
        Assert.Equal(Now.AddDays(-1), account.CreatedAt);
    }
}
