using Dapper;
using FinCtrl.Common.Infrastructure;
using FinCtrl.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                 ,a.CreatedAt
                 ,a.LastUpdatedAt
              FROM Accounts a";

        public async Task<List<Account>> GetAll()
        {
            var conn = await _connectionFactory.GetConnectionAsync();

            return (await conn.QueryAsync(_select + " ORDER BY a.AccountId DESC"))
                .Select(x => new Account(x.AccountId, x.ExcelAccountName, x.DisplayName, x.CreatedAt, x.LastUpdatedAt))
                .ToList();
        }

        public async Task<Account?> GetById(int accountId)
        {
            var conn = await _connectionFactory.GetConnectionAsync();

            return (await conn.QueryAsync(_select + " WHERE a.AccountId = @accountId", new { accountId }))
                .Select(x => new Account(x.AccountId, x.ExcelAccountName, x.DisplayName, x.CreatedAt, x.LastUpdatedAt))
                .SingleOrDefault();
        }

        public async Task<Account?> GetByExcelName(string excelAccountName)
        {
            var conn = await _connectionFactory.GetConnectionAsync();

            return (await conn.QueryAsync(_select + " WHERE a.ExcelAccountName = @excelAccountName", new { excelAccountName }))
                .Select(x => new Account(x.AccountId, x.ExcelAccountName, x.DisplayName, x.CreatedAt, x.LastUpdatedAt))
                .FirstOrDefault();
        }

        public async Task<bool> Exists(string excelAccountName)
        {
            return await GetByExcelName(excelAccountName) is not null;
        }

        public async Task<int> Create(Account account)
        {
            var sql = @"DECLARE @generated_keys table([Id] int)

INSERT INTO dbo.Accounts (ExcelAccountName
                  , DisplayName
                  , CreatedAt
                  , LastUpdatedAt)
  OUTPUT INSERTED.AccountId INTO @generated_keys
                  VALUES (@ExcelAccountName, @DisplayName, @CreatedAt, @LastUpdatedAt);

SELECT * FROM @generated_keys";

            var conn = await _connectionFactory.GetConnectionAsync();

            return conn.ExecuteScalar<int>(sql, new
            {
                account.ExcelAccountName,
                account.DisplayName,
                account.CreatedAt,
                account.LastUpdatedAt
            });
        }

        public async Task Update(params Account[] accounts)
        {
            var sql = @"UPDATE Accounts
                        SET ExcelAccountName = @ExcelAccountName
                           ,DisplayName = @DisplayName
                           ,LastUpdatedAt = @LastUpdatedAt
                        WHERE AccountId = @AccountId;";

            foreach (var account in accounts)
            {
                await Execute(sql, new
                {
                    account.AccountId,
                    account.ExcelAccountName,
                    account.DisplayName,
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
