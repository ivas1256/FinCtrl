using FinCtrl.Application.Accounts;
using FinCtrl.Common.Infrastructure;
using FinCtrl.Domain;

namespace FinCtrl.Infrastructure.Accounts
{
    internal class AccountRepository : RepositoryBase, IAccountRepository
    {
        public AccountRepository(IDbConnectionFactory dbConnectionFactory) : base(dbConnectionFactory)
        {
        }

        private const string _select = @"SELECT
                  a.AccountId
                 ,a.ExcelAccountName
                 ,a.DisplayName
                 ,a.IsSavings
                 ,a.CreatedAt
                 ,a.LastUpdatedAt
              FROM Accounts a";

        public async Task<List<Account>> GetAll()
        {
            return (await Query<Account>(_select + " ORDER BY a.AccountId DESC")).ToList();
        }

        public async Task<Account?> GetById(int accountId)
        {
            return await FirstOrDefault<Account>(_select + " WHERE a.AccountId = @accountId", new { accountId });
        }

        public async Task<Account?> GetByExcelName(string excelAccountName)
        {
            return await FirstOrDefault<Account>(_select + " WHERE a.ExcelAccountName = @excelAccountName", new { excelAccountName });
        }

        public async Task<bool> Exists(string excelAccountName)
        {
            return await GetByExcelName(excelAccountName) is not null;
        }

        public async Task<int> CountPayments(int accountId)
        {
            // Без фильтра IsDeleted: удалённые операции тоже держат внешний ключ FK_Payments_Accounts_AccountId
            return await ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.Payments WHERE AccountId = @accountId", new { accountId });
        }

        public async Task<int> Create(Account account)
        {
            var sql = @"INSERT INTO dbo.Accounts (ExcelAccountName, DisplayName, IsSavings, CreatedAt, LastUpdatedAt)
                        OUTPUT INSERTED.AccountId
                        VALUES (@ExcelAccountName, @DisplayName, @IsSavings, @CreatedAt, @LastUpdatedAt);";

            return await ExecuteScalar<int>(sql, new
            {
                account.ExcelAccountName,
                account.DisplayName,
                account.IsSavings,
                account.CreatedAt,
                account.LastUpdatedAt
            });
        }

        public async Task Update(params Account[] accounts)
        {
            var sql = @"UPDATE Accounts
                        SET ExcelAccountName = @ExcelAccountName
                           ,DisplayName = @DisplayName
                           ,IsSavings = @IsSavings
                           ,LastUpdatedAt = @LastUpdatedAt
                        WHERE AccountId = @AccountId;";

            foreach (var account in accounts)
            {
                await Execute(sql, new
                {
                    account.AccountId,
                    account.ExcelAccountName,
                    account.DisplayName,
                    account.IsSavings,
                    account.LastUpdatedAt
                });
            }
        }

        public async Task Delete(params Account[] accounts)
        {
            var sql = @"DELETE FROM dbo.Accounts WHERE AccountId = @AccountId;";

            foreach (var account in accounts)
            {
                await Execute(sql, new { account.AccountId });
            }
        }
    }
}
