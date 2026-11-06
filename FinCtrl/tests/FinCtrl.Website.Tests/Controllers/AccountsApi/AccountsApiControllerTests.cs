using FinCtrl.Application.Accounts;
using FinCtrl.Domain;
using FinCtrl.Website.Controllers.AccountsApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FinCtrl.Website.Tests.Controllers.AccountsApi;

public class AccountsApiControllerTests
{
    private static readonly DateTime Now = new(2025, 10, 15, 14, 30, 0);

    private readonly IAccountService _accountService = Substitute.For<IAccountService>();
    private readonly AccountsApiController _controller;

    public AccountsApiControllerTests()
    {
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetLocalTimeZone(TimeZoneInfo.Utc);
        timeProvider.SetUtcNow(new DateTimeOffset(Now, TimeSpan.Zero));
        _controller = new AccountsApiController(_accountService, timeProvider);
    }

    [Fact]
    public async Task Create_Dto_CreatesAccountWithDisplayNameAndTimeAndReturnsSaved()
    {
        _accountService.Create(Arg.Any<Account>()).Returns(5);
        _accountService.GetById(5).Returns(new Account(5, "Карта *1234", "Основная", Now, null));
        var dto = new AccountDto { AccountId = 99, ExcelAccountName = "Карта *1234", DisplayName = "Основная", IsSavings = true };

        var result = await _controller.Create(dto);

        await _accountService.Received(1).Create(Arg.Is<Account>(a =>
            a.AccountId == 0 && a.ExcelAccountName == "Карта *1234" && a.DisplayName == "Основная" && a.IsSavings && a.CreatedAt == Now));
        var saved = Assert.IsType<AccountDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal((5, "Основная", Now), (saved.AccountId, saved.DisplayName, saved.CreatedAt));
    }

    [Fact]
    public async Task Update_RouteId_UpdatesAccountWithRouteIdAndReturnsNoContent()
    {
        var dto = new AccountDto { AccountId = 99, ExcelAccountName = "Карта *1234", DisplayName = "Основная", IsSavings = true };

        var result = await _controller.Update(5, dto);

        Assert.IsType<NoContentResult>(result);
        await _accountService.Received(1).Update(Arg.Is<Account>(a =>
            a.AccountId == 5 && a.ExcelAccountName == "Карта *1234" && a.DisplayName == "Основная" && a.IsSavings));
    }

    [Fact]
    public async Task Delete_Id_DeletesAndReturnsNoContent()
    {
        var result = await _controller.Delete(5);

        Assert.IsType<NoContentResult>(result);
        await _accountService.Received(1).Delete(5);
    }
}
