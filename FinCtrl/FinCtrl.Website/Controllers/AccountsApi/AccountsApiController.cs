using FinCtrl.Application.Accounts;
using FinCtrl.Domain;
using FinCtrl.Website.RequestDto;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace FinCtrl.Website.Controllers.AccountsApi
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsApiController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountsApiController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok((await _accountService.GetAll())
                .Select(AccountDto.FromModel));
        }

        [HttpPost]
        public async Task<IActionResult> Create(AccountDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ExcelAccountName) || string.IsNullOrWhiteSpace(dto.DisplayName))
                return BadRequest("Название счета в excel файле и отображаемое название обязательны");

            if (await _accountService.Exists(dto.ExcelAccountName))
                return BadRequest("Счет с таким названием в excel файле уже существует");

            var newAccount = Account.CreateNew(dto.ExcelAccountName);
            newAccount.DisplayName = dto.DisplayName;
            var id = await _accountService.Create(newAccount);

            return Ok(AccountDto.FromModel(await _accountService.GetById(id)));
        }

        [HttpPut("{accountId}")]
        public async Task<IActionResult> Update(int accountId, AccountDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ExcelAccountName) || string.IsNullOrWhiteSpace(dto.DisplayName))
                return BadRequest("Название счета в excel файле и отображаемое название обязательны");

            var toUpdate = Account.CreateFromExisting(accountId, dto.ExcelAccountName, dto.DisplayName);
            await _accountService.Update(toUpdate);

            return Ok();
        }

        [HttpDelete("{accountId}")]
        public async Task<IActionResult> Delete(int accountId)
        {
            var account = await _accountService.GetById(accountId);
            if (account is null)
                return NotFound();

            await _accountService.Delete(account);

            return Ok();
        }
    }
}
