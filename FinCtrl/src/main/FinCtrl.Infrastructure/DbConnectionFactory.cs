using FinCtrl.Common.Infrastructure;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Infrastructure
{
    internal class DbConnectionFactory : IDbConnectionFactory, IDisposable
    {
        private readonly IConfiguration _configuration;
        private SqlConnection _connection;

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
