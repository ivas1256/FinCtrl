using System.Data.SqlClient;

namespace FinCtrl.Common.Infrastructure
{
    public interface IDbConnectionFactory
    {
        public Task<SqlConnection> GetConnectionAsync();
    }
}
