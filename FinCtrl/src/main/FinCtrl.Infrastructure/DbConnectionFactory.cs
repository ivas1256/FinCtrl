using FinCtrl.Common.Infrastructure;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace FinCtrl.Infrastructure
{
    internal class DbConnectionFactory : IDbConnectionFactory, IDisposable
    {
        private readonly IConfiguration _configuration;
        private SqlConnection? _connection;

        public DbConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<SqlConnection> GetConnectionAsync()
        {
            if (_connection is not null)
            {
                return _connection;
            }

            var connStr = _configuration.GetConnectionString("DefaultDatabase");

            _connection = new SqlConnection(connStr);
            await _connection.OpenAsync();
            return _connection;
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _connection = null;
        }
    }
}
