using Microsoft.Data.SqlClient;

namespace FinCtrl.Common.Infrastructure
{
    /// <summary>Соединение с БД на scope (запрос) и его текущая транзакция.</summary>
    public interface IDbConnectionFactory : IUnitOfWork
    {
        /// <summary>Открытое соединение scope (создаётся при первом обращении).</summary>
        public Task<SqlConnection> GetConnectionAsync();

        /// <summary>Текущая транзакция соединения или <c>null</c>, если она не открыта.</summary>
        public SqlTransaction? CurrentTransaction { get; }
    }
}
