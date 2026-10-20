using FinCtrl.Application.Accounts;
using FinCtrl.Domain;
using FinCtrl.Website.RequestDto;
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

        /// <summary>Создаёт счёт; 409 — счёт с таким названием в файле уже есть.</summary>
        [HttpPost]
        public async Task<IActionResult> Create(AccountDto dto)
        {
            var newAccount = Account.CreateNew(dto.ExcelAccountName, _timeProvider.GetLocalNow().DateTime);
            newAccount.DisplayName = dto.DisplayName;
            var id = await _accountService.Create(newAccount);

            return Ok(AccountDto.FromModel(await _accountService.GetById(id)));
        }

        /// <summary>Правит названия счёта; 404 — счёт не найден, 409 — название в файле занято.</summary>
        [HttpPut("{accountId}")]
        public async Task<IActionResult> Update(int accountId, AccountDto dto)
        {
            var toUpdate = Account.CreateFromExisting(accountId, dto.ExcelAccountName, dto.DisplayName);
            await _accountService.Update(toUpdate);

            return Ok();
        }

        /// <summary>Удаляет счёт; 404 — счёт не найден, 409 — счёт указан в операциях.</summary>
        [HttpDelete("{accountId}")]
        public async Task<IActionResult> Delete(int accountId)
        {
            await _accountService.Delete(accountId);

            return Ok();
        }
    }
}
