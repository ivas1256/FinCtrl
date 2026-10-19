using FinCtrl.Domain;
using FinCtrl.Infrastructure.Accounts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinCtrl.Application.Accounts
{
    internal class AccountService : IAccountService
    {
        private readonly IAccountRepository _repository;

        public AccountService(IAccountRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Account>> GetAll()
        {
            return await _repository.GetAll();
        }

        public async Task<Account?> GetById(int accountId)
        {
            return await _repository.GetById(accountId);
        }

        public async Task<Account?> GetByExcelName(string excelAccountName)
        {
            return await _repository.GetByExcelName(excelAccountName);
        }

        public async Task<int> Create(Account account)
        {
            return await _repository.Create(account);
        }

        public async Task Update(params Account[] accounts)
        {
            foreach (var account in accounts)
            {
                account.Updated();
            }

            await _repository.Update(accounts);
        }

        public async Task Delete(params Account[] accounts)
        {
            await _repository.Delete(accounts);
        }

        public async Task<bool> Exists(string excelAccountName)
        {
            return await _repository.Exists(excelAccountName);
        }
    }
}
