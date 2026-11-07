using FinCtrl.Common.Application;
using FinCtrl.Domain;

namespace FinCtrl.Application.Accounts
{
    internal class AccountService : IAccountService
    {
        // Длина колонок ExcelAccountName/DisplayName в БД (NVARCHAR(255))
        internal const int MaxNameLength = 255;

        // Длина колонки AccountNumber в БД (NVARCHAR(50))
        internal const int MaxNumberLength = 50;

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

        public async Task<Account?> GetWithoutNumberByExcelName(string excelAccountName)
        {
            return await _repository.GetWithoutNumberByExcelName(excelAccountName);
        }

        public async Task<Account?> GetByAccountNumber(string accountNumber)
        {
            return await _repository.GetByAccountNumber(accountNumber);
        }

        public async Task<int> Create(Account account)
        {
            Validate(account);
            await EnsureNumberIsFree(account);

            return await _repository.Create(account);
        }

        public async Task Update(Account account)
        {
            Validate(account);
            await GetById(account.AccountId);
            await EnsureNumberIsFree(account);

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

        // Ключ счёта — номер (с задачи 0038); названия могут повторяться.
        private async Task EnsureNumberIsFree(Account account)
        {
            if (account.AccountNumber is null)
                return;

            var sameNumber = await _repository.GetByAccountNumber(account.AccountNumber);
            if (sameNumber is not null && sameNumber.AccountId != account.AccountId)
                throw new ConflictException($"Счёт с номером «{account.AccountNumber}» уже существует");
        }

        private static void Validate(Account account)
        {
            account.AccountNumber = Account.NormalizeAccountNumber(account.AccountNumber);

            if (string.IsNullOrWhiteSpace(account.ExcelAccountName) || string.IsNullOrWhiteSpace(account.DisplayName))
                throw new ValidationException("Название счёта в excel-файле и отображаемое название обязательны");

            if (account.ExcelAccountName.Length > MaxNameLength || account.DisplayName.Length > MaxNameLength)
                throw new ValidationException($"Названия счёта должны быть не длиннее {MaxNameLength} символов");

            if (account.AccountNumber?.Length > MaxNumberLength)
                throw new ValidationException($"Номер счёта должен быть не длиннее {MaxNumberLength} символов");
        }
    }
}
