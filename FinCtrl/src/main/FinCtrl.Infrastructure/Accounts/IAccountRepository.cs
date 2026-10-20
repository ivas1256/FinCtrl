using FinCtrl.Domain;

namespace FinCtrl.Infrastructure.Accounts
{
    public interface IAccountRepository
    {
        Task<List<Account>> GetAll();
        Task<Account?> GetById(int accountId);
        Task<Account?> GetByExcelName(string excelAccountName);

        Task<int> Create(Account account);
        Task Update(params Account[] accounts);
        Task Delete(params Account[] accounts);

        Task<bool> Exists(string excelAccountName);
    }
}
