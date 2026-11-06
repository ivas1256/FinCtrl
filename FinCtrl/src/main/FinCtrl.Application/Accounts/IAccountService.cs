using FinCtrl.Domain;

namespace FinCtrl.Application.Accounts
{
    public interface IAccountService
    {
        /// <summary>Все счета, новые первыми.</summary>
        Task<List<Account>> GetAll();

        /// <summary>Счёт по id.</summary>
        /// <exception cref="FinCtrl.Common.Application.NotFoundException">Счёт не найден.</exception>
        Task<Account> GetById(int accountId);

        /// <summary>Счёт по имени из банковского файла или <c>null</c>, если его нет (импорт строки без номера счёта).</summary>
        Task<Account?> GetByExcelName(string excelAccountName);

        /// <summary>Счёт с таким именем из банковского файла и без номера или <c>null</c> (счёт, созданный до появления номеров).</summary>
        Task<Account?> GetWithoutNumberByExcelName(string excelAccountName);

        /// <summary>Счёт по номеру или <c>null</c>, если его нет (get-or-create при импорте).</summary>
        Task<Account?> GetByAccountNumber(string accountNumber);

        /// <summary>Создаёт счёт.</summary>
        /// <returns>Id созданного счёта.</returns>
        /// <exception cref="FinCtrl.Common.Application.ValidationException">Не заполнены названия или названия/номер слишком длинные.</exception>
        /// <exception cref="FinCtrl.Common.Application.ConflictException">Счёт с таким номером уже есть.</exception>
        Task<int> Create(Account account);

        /// <summary>Сохраняет названия, номер и признак «накопительный» счёта.</summary>
        /// <exception cref="FinCtrl.Common.Application.NotFoundException">Счёт не найден.</exception>
        /// <exception cref="FinCtrl.Common.Application.ValidationException">Не заполнены названия или названия/номер слишком длинные.</exception>
        /// <exception cref="FinCtrl.Common.Application.ConflictException">Номер занят другим счётом.</exception>
        Task Update(Account account);

        /// <summary>Удаляет счёт.</summary>
        /// <exception cref="FinCtrl.Common.Application.NotFoundException">Счёт не найден.</exception>
        /// <exception cref="FinCtrl.Common.Application.ConflictException">Счёт указан в операциях.</exception>
        Task Delete(int accountId);
    }
}
