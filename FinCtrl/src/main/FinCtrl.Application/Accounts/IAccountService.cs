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

        /// <summary>Счёт по имени из банковского файла или <c>null</c>, если его нет (get-or-create при импорте).</summary>
        Task<Account?> GetByExcelName(string excelAccountName);

        /// <summary>Создаёт счёт.</summary>
        /// <returns>Id созданного счёта.</returns>
        /// <exception cref="FinCtrl.Common.Application.ValidationException">Не заполнены названия или они слишком длинные.</exception>
        /// <exception cref="FinCtrl.Common.Application.ConflictException">Счёт с таким названием в файле уже есть.</exception>
        Task<int> Create(Account account);

        /// <summary>Сохраняет названия счёта.</summary>
        /// <exception cref="FinCtrl.Common.Application.NotFoundException">Счёт не найден.</exception>
        /// <exception cref="FinCtrl.Common.Application.ValidationException">Не заполнены названия или они слишком длинные.</exception>
        /// <exception cref="FinCtrl.Common.Application.ConflictException">Название в файле занято другим счётом.</exception>
        Task Update(Account account);

        /// <summary>Удаляет счёт.</summary>
        /// <exception cref="FinCtrl.Common.Application.NotFoundException">Счёт не найден.</exception>
        /// <exception cref="FinCtrl.Common.Application.ConflictException">Счёт указан в операциях.</exception>
        Task Delete(int accountId);
    }
}
