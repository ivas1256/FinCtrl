using System.Data;
using System.Configuration;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace FinCtrl.DAL
{
    public class DapperDBContext
    {
        string _connectionString;

        public DapperDBContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultDatabase");
        }

        public SqlConnection SqlConnection
        {
            get => new SqlConnection(_connectionString);
        }

        public IEnumerable<T> Query<T>(string sql, object qParams)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                return conn.Query<T>(sql, qParams);
            }
        }
    }
}
