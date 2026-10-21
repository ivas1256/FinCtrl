using FinCtrl.Common.Application;
using FinCtrl.Domain;

namespace FinCtrl.Application.Accounts
{
    internal class AccountService : IAccountService
    {
        // Длина колонок ExcelAccountName/DisplayName в БД (NVARCHAR(255))
        internal const int MaxNameLength = 255;

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

        public async Task<Account> GetById(int accountId)
        {
            return await _repository.GetById(accountId)
                ?? throw new NotFoundException($"Счёт {accountId} не найден");
        }

        public async Task<Account?> GetByExcelName(string excelAccountName)
        {
            return await _repository.GetByExcelName(excelAccountName);
        }

        public async Task<int> Create(Account account)
        {
            ValidateNames(account);

            if (await _repository.Exists(account.ExcelAccountName))
                throw new ConflictException($"Счёт с названием в файле «{account.ExcelAccountName}» уже существует");

            return await _repository.Create(account);
        }

        public async Task Update(Account account)
        {
            ValidateNames(account);
            await GetById(account.AccountId);

            var sameName = await _repository.GetByExcelName(account.ExcelAccountName);
            if (sameName is not null && sameName.AccountId != account.AccountId)
                throw new ConflictException($"Счёт с названием в файле «{account.ExcelAccountName}» уже существует");

            account.Updated(_timeProvider.GetLocalNow().DateTime);
            await _repository.Update(account);
        }

        public async Task Delete(int accountId)
        {
            var account = await GetById(accountId);

            var paymentCount = await _repository.CountPayments(accountId);
            if (paymentCount > 0)
                throw new ConflictException(
                    $"Счёт «{account.DisplayName}» указан в операциях ({paymentCount}), удалить его нельзя. Сначала снимите счёт с этих операций.");

            await _repository.Delete(account);
        }

        private static void ValidateNames(Account account)
        {
            if (string.IsNullOrWhiteSpace(account.ExcelAccountName) || string.IsNullOrWhiteSpace(account.DisplayName))
                throw new ValidationException("Название счёта в excel-файле и отображаемое название обязательны");

            if (account.ExcelAccountName.Length > MaxNameLength || account.DisplayName.Length > MaxNameLength)
                throw new ValidationException($"Названия счёта должны быть не длиннее {MaxNameLength} символов");
        }
    }
}
