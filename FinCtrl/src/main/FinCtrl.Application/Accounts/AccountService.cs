using FinCtrl.Domain;
using FinCtrl.Infrastructure.Accounts;

namespace FinCtrl.Application.Accounts
{
    internal class AccountService : IAccountService
    {
        private readonly IAccountRepository _repository;
        private readonly TimeProvider _timeProvider;

        public AccountService(IAccountRepository repository, TimeProvider timeProvider)
        {
            _repository = repository;
            _timeProvider = timeProvider;
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
            var now = _timeProvider.GetLocalNow().DateTime;
            foreach (var account in accounts)
            {
                account.Updated(now);
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
