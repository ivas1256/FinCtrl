using FinCtrl.Domain;

namespace FinCtrl.Application.Accounts;

/// <summary>Хранилище банковских счетов.</summary>
public interface IAccountRepository
{
    /// <summary>Все счета, новые первыми.</summary>
    Task<List<Account>> GetAll();

    /// <summary>Счёт по id или <c>null</c>, если не найден.</summary>
    Task<Account?> GetById(int accountId);

    /// <summary>Счёт по имени из банковского файла или <c>null</c>, если не найден.</summary>
    Task<Account?> GetByExcelName(string excelAccountName);

    /// <summary>Создаёт счёт.</summary>
    /// <returns>Id созданного счёта.</returns>
    Task<int> Create(Account account);

    /// <summary>Сохраняет изменения счетов.</summary>
    Task Update(params Account[] accounts);

    /// <summary>Удаляет счета.</summary>
    Task Delete(params Account[] accounts);

    /// <summary>Число операций со счётом, включая удалённые (на них тоже ссылается внешний ключ).</summary>
    Task<int> CountPayments(int accountId);

    /// <summary>Есть ли счёт с таким именем из банковского файла.</summary>
    Task<bool> Exists(string excelAccountName);
}
