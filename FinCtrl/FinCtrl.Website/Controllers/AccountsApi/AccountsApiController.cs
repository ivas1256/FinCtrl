using FinCtrl.Application.Accounts;
using FinCtrl.Domain;
using Microsoft.AspNetCore.Mvc;

namespace FinCtrl.Website.Controllers.AccountsApi
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsApiController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly TimeProvider _timeProvider;

        public AccountsApiController(IAccountService accountService, TimeProvider timeProvider)
        {
            _accountService = accountService;
            _timeProvider = timeProvider;
        }

        /// <summary>Все счета, новые первыми.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok((await _accountService.GetAll())
                .Select(AccountDto.FromModel));
        }

        /// <summary>Создаёт счёт; 409 — счёт с таким номером уже есть.</summary>
        [HttpPost]
        public async Task<IActionResult> Create(AccountDto dto)
        {
            var newAccount = Account.CreateNew(dto.ExcelAccountName, _timeProvider.GetLocalNow().DateTime, dto.AccountNumber);
            newAccount.DisplayName = dto.DisplayName;
            newAccount.IsSavings = dto.IsSavings;
            var id = await _accountService.Create(newAccount);

            return Ok(AccountDto.FromModel(await _accountService.GetById(id)));
        }

        /// <summary>Правит названия, номер и признак «накопительный» счёта; 404 — счёт не найден, 409 — номер занят.</summary>
        [HttpPut("{accountId}")]
        public async Task<IActionResult> Update(int accountId, AccountDto dto)
        {
            var toUpdate = Account.CreateFromExisting(accountId, dto.ExcelAccountName, dto.DisplayName, dto.IsSavings, dto.AccountNumber);
            await _accountService.Update(toUpdate);

            return NoContent();
        }

        /// <summary>Удаляет счёт; 404 — счёт не найден, 409 — счёт указан в операциях.</summary>
        [HttpDelete("{accountId}")]
        public async Task<IActionResult> Delete(int accountId)
        {
            await _accountService.Delete(accountId);

            return NoContent();
        }
    }
}
