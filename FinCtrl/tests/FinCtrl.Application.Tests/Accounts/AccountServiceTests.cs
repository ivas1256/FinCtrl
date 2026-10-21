using FinCtrl.Application.Accounts;
using FinCtrl.Common.Application;
using FinCtrl.Domain;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FinCtrl.Application.Tests.Accounts;

public class AccountServiceTests
{
    private static readonly DateTime Now = new(2025, 10, 15, 14, 30, 0);

    private readonly IAccountRepository _repository = Substitute.For<IAccountRepository>();
    private readonly AccountService _service;
    private readonly Account _card = new(5, "Карта", "Моя карта", Now, null);

    public AccountServiceTests()
    {
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetLocalTimeZone(TimeZoneInfo.Utc);
        timeProvider.SetUtcNow(new DateTimeOffset(Now, TimeSpan.Zero));

        _repository.GetById(5).Returns(_card);
        _repository.GetByExcelName("Карта").Returns(_card);

        _service = new AccountService(_repository, timeProvider);
    }

    [Fact]
    public async Task GetById_UnknownAccount_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetById(99));
    }

    [Fact]
    public async Task Create_NewName_CreatesAccount()
    {
        _repository.Create(Arg.Any<Account>()).Returns(6);

        var id = await _service.Create(Account.CreateNew("Вклад", Now));

        Assert.Equal(6, id);
        await _repository.Received(1).Create(Arg.Is<Account>(a => a.ExcelAccountName == "Вклад"));
    }

    [Fact]
    public async Task Create_DuplicateExcelName_ThrowsConflictException()
    {
        _repository.Exists("Карта").Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.Create(Account.CreateNew("Карта", Now)));

        await _repository.DidNotReceiveWithAnyArgs().Create(default!);
    }

    [Theory]
    [InlineData("", "Карта")]
    [InlineData("Карта", " ")]
    public async Task Create_EmptyNames_ThrowsValidationException(string excelName, string displayName)
    {
        var account = Account.CreateNew(excelName, Now);
        account.DisplayName = displayName;

        await Assert.ThrowsAsync<ValidationException>(() => _service.Create(account));

        await _repository.DidNotReceiveWithAnyArgs().Create(default!);
    }

    [Fact]
    public async Task Create_TooLongName_ThrowsValidationException()
    {
        var account = Account.CreateNew(new string('а', AccountService.MaxNameLength + 1), Now);

        await Assert.ThrowsAsync<ValidationException>(() => _service.Create(account));
    }

    [Fact]
    public async Task Update_ExistingAccount_SavesWithUpdatedAt()
    {
        await _service.Update(Account.CreateFromExisting(5, "Карта", "Новое имя"));

        await _repository.Received(1).Update(Arg.Is<Account[]>(a =>
            a.Length == 1 && a[0].DisplayName == "Новое имя" && a[0].LastUpdatedAt == Now));
    }

    [Fact]
    public async Task Update_UnknownAccount_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.Update(Account.CreateFromExisting(99, "Вклад", "Вклад")));

        await _repository.DidNotReceiveWithAnyArgs().Update();
    }

    [Fact]
    public async Task Update_ExcelNameOfAnotherAccount_ThrowsConflictException()
    {
        _repository.GetById(6).Returns(new Account(6, "Вклад", "Вклад", Now, null));

        await Assert.ThrowsAsync<ConflictException>(() => _service.Update(Account.CreateFromExisting(6, "Карта", "Вклад")));

        await _repository.DidNotReceiveWithAnyArgs().Update();
    }

    [Fact]
    public async Task Update_EmptyDisplayName_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.Update(Account.CreateFromExisting(5, "Карта", "")));
    }

    [Fact]
    public async Task Delete_AccountWithoutPayments_Deletes()
    {
        _repository.CountPayments(5).Returns(0);

        await _service.Delete(5);

        await _repository.Received(1).Delete(Arg.Is<Account[]>(a => a.Length == 1 && a[0].AccountId == 5));
    }

    [Fact]
    public async Task Delete_AccountWithPayments_ThrowsConflictExceptionWithCount()
    {
        _repository.CountPayments(5).Returns(12);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.Delete(5));

        Assert.Contains("12", ex.Message);
        await _repository.DidNotReceiveWithAnyArgs().Delete();
    }

    [Fact]
    public async Task Delete_UnknownAccount_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.Delete(99));

        await _repository.DidNotReceiveWithAnyArgs().Delete();
    }
}
